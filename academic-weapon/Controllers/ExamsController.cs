using academic_weapon.Data;
using academic_weapon.Models;
using academic_weapon.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace academic_weapon.Controllers;

// Community "Exam Autopsy" archive. Browsing (Index/Details/Download) is public;
// uploading, editing, deleting and rating require a logged-in account.
// Unlike the POST-only child controllers, this is a top-level browsable entity
// with full CRUD + its own views.
public class ExamsController : Controller
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".png", ".jpg", ".jpeg" };
    private const long MaxFileBytes = 10 * 1024 * 1024; // 10 MB

    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public ExamsController(AppDbContext context, UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
    }

    // GET /Exams?course=&professor=&year=
    public async Task<IActionResult> Index(string? course, string? professor, int? year)
    {
        var query = _context.Exams.Include(e => e.UploadedBy).AsQueryable();

        if (!string.IsNullOrWhiteSpace(course))
            query = query.Where(e => e.CourseName == course);
        if (!string.IsNullOrWhiteSpace(professor))
            query = query.Where(e => e.Professor == professor);
        if (year.HasValue)
            query = query.Where(e => e.Year == year.Value);

        var exams = await query
            .OrderByDescending(e => e.Year)
            .ThenBy(e => e.CourseName)
            .Select(e => new ExamListItem
            {
                Exam = e,
                UploaderName = e.UploadedBy != null ? e.UploadedBy.DisplayName : "Unknown",
                RatingCount = e.Ratings.Count,
                AverageDifficulty = e.Ratings.Any() ? e.Ratings.Average(r => (double)r.Score) : (double?)null
            })
            .ToListAsync();

        var vm = new ExamIndexViewModel
        {
            Exams = exams,
            Course = course,
            Professor = professor,
            Year = year,
            CourseOptions = await _context.Exams.Select(e => e.CourseName).Distinct().OrderBy(c => c).ToListAsync(),
            ProfessorOptions = await _context.Exams.Select(e => e.Professor).Distinct().OrderBy(p => p).ToListAsync(),
            YearOptions = await _context.Exams.Select(e => e.Year).Distinct().OrderByDescending(y => y).ToListAsync()
        };
        return View(vm);
    }

    // GET /Exams/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var exam = await _context.Exams
            .Include(e => e.UploadedBy)
            .Include(e => e.Ratings)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (exam == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        var vm = new ExamDetailsViewModel
        {
            Exam = exam,
            UploaderName = exam.UploadedBy?.DisplayName ?? "Unknown",
            RatingCount = exam.Ratings.Count,
            AverageDifficulty = exam.Ratings.Any() ? exam.Ratings.Average(r => (double)r.Score) : null,
            UserRating = currentUserId == null
                ? null
                : exam.Ratings.FirstOrDefault(r => r.UserId == currentUserId)?.Score,
            CanManage = currentUserId != null && exam.UploadedById == currentUserId
        };
        return View(vm);
    }

    [Authorize]
    public IActionResult Create() => View(new Exam { Year = DateTime.UtcNow.Year });

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Exam exam, IFormFile? file)
    {
        ValidateUpload(file, required: true);
        if (!ModelState.IsValid) return View(exam);

        exam.FilePath = await SaveFileAsync(file!);
        exam.OriginalFileName = Path.GetFileName(file!.FileName);
        exam.ContentType = file.ContentType;
        exam.UploadedById = _userManager.GetUserId(User)!;
        exam.UploadedAt = DateTime.UtcNow;

        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = exam.Id });
    }

    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var exam = await _context.Exams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsOwner(exam)) return Forbid();
        return View(exam);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Exam form, IFormFile? file)
    {
        var exam = await _context.Exams.FindAsync(id);
        if (exam == null) return NotFound();
        if (!IsOwner(exam)) return Forbid();

        // A replacement file is optional on edit.
        ValidateUpload(file, required: false);
        if (!ModelState.IsValid) return View(exam);

        exam.CourseName = form.CourseName;
        exam.CourseCode = form.CourseCode;
        exam.Professor = form.Professor;
        exam.Year = form.Year;
        exam.ExamType = form.ExamType;
        exam.Description = form.Description;

        if (file is { Length: > 0 })
        {
            DeletePhysicalFile(exam.FilePath);
            exam.FilePath = await SaveFileAsync(file);
            exam.OriginalFileName = Path.GetFileName(file.FileName);
            exam.ContentType = file.ContentType;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = exam.Id });
    }

    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var exam = await _context.Exams.Include(e => e.UploadedBy).FirstOrDefaultAsync(e => e.Id == id);
        if (exam == null) return NotFound();
        if (!IsOwner(exam)) return Forbid();
        return View(exam);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var exam = await _context.Exams.FindAsync(id);
        if (exam == null) return RedirectToAction(nameof(Index));
        if (!IsOwner(exam)) return Forbid();

        DeletePhysicalFile(exam.FilePath);
        // Ratings are removed via cascade delete configured in AppDbContext.
        _context.Exams.Remove(exam);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET /Exams/Download/5 — streams the file with its original name. Public.
    public async Task<IActionResult> Download(int id)
    {
        var exam = await _context.Exams.FindAsync(id);
        if (exam == null) return NotFound();

        var absolute = Path.Combine(_env.WebRootPath, exam.FilePath.TrimStart('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(absolute)) return NotFound();

        var bytes = await System.IO.File.ReadAllBytesAsync(absolute);
        var contentType = string.IsNullOrEmpty(exam.ContentType) ? "application/octet-stream" : exam.ContentType;
        return File(bytes, contentType, exam.OriginalFileName);
    }

    // POST /Exams/Rate — upsert the current user's 1-5 difficulty vote.
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(int examId, int score)
    {
        if (score < 1 || score > 5) return RedirectToAction(nameof(Details), new { id = examId });
        if (!await _context.Exams.AnyAsync(e => e.Id == examId)) return NotFound();

        var userId = _userManager.GetUserId(User)!;
        var rating = await _context.Ratings.FirstOrDefaultAsync(r => r.ExamId == examId && r.UserId == userId);
        if (rating == null)
        {
            _context.Ratings.Add(new Rating { ExamId = examId, UserId = userId, Score = score });
        }
        else
        {
            rating.Score = score;
            rating.CreatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = examId });
    }

    // ---- helpers ----

    private bool IsOwner(Exam exam) => exam.UploadedById == _userManager.GetUserId(User);

    private void ValidateUpload(IFormFile? file, bool required)
    {
        if (file is null || file.Length == 0)
        {
            if (required) ModelState.AddModelError("file", "Please attach the exam file (PDF or image).");
            return;
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            ModelState.AddModelError("file", "Only PDF, PNG, JPG or JPEG files are allowed.");
        if (file.Length > MaxFileBytes)
            ModelState.AddModelError("file", "File is too large (max 10 MB).");
    }

    private async Task<string> SaveFileAsync(IFormFile file)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "exams");
        Directory.CreateDirectory(dir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var name = $"{Guid.NewGuid():N}{ext}";
        var absolute = Path.Combine(dir, name);

        await using var stream = new FileStream(absolute, FileMode.Create);
        await file.CopyToAsync(stream);

        // Web-relative path used for both the static link and Download lookup.
        return $"/uploads/exams/{name}";
    }

    private void DeletePhysicalFile(string? webPath)
    {
        if (string.IsNullOrEmpty(webPath)) return;
        var absolute = Path.Combine(_env.WebRootPath,
            webPath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(absolute)) System.IO.File.Delete(absolute);
    }
}
