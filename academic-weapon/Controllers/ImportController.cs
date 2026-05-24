using academic_weapon.Data;
using academic_weapon.Models;
using academic_weapon.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace academic_weapon.Controllers;

public class ImportController : Controller
{
    private readonly AppDbContext _context;
    private readonly FinkiImportService _finki;

    public ImportController(AppDbContext context, FinkiImportService finki)
    {
        _context = context;
        _finki = finki;
    }

    public IActionResult Index() => View();

    // Automated CAS import
    [HttpPost]
    public async Task<IActionResult> Finki(string username, string password, string coursesUrl)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Username and password are required.";
            return RedirectToAction("Index");
        }

        coursesUrl = string.IsNullOrWhiteSpace(coursesUrl)
            ? "https://courses.finki.ukim.mk/my/"
            : coursesUrl.Trim();

        var result = await _finki.ImportAsync(username, password, coursesUrl);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            TempData["DebugHtml"] = result.DebugHtmlSnippet;
            return RedirectToAction("Index");
        }

        var added = SaveCourses(result.Courses);
        TempData["ImportResult"] = $"Imported {added} subject(s). {result.Courses.Count - added} skipped (already exist).";
        return RedirectToAction("Index", "Subjects");
    }

    // Manual JSON fallback
    [HttpPost]
    public IActionResult Manual(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            TempData["Error"] = "Paste the JSON from the browser script.";
            return RedirectToAction("Index");
        }

        List<ImportedCourse> courses;
        try
        {
            courses = JsonSerializer.Deserialize<List<ImportedCourse>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        catch
        {
            TempData["Error"] = "Invalid JSON — make sure you copied the full output.";
            return RedirectToAction("Index");
        }

        var added = SaveCourses(courses);
        TempData["ImportResult"] = $"Imported {added} subject(s). {courses.Count - added} skipped (already exist).";
        return RedirectToAction("Index", "Subjects");
    }

    private int SaveCourses(List<ImportedCourse> courses)
    {
        var existingNames = _context.Subjects.Select(s => s.Name.ToLower()).ToHashSet();
        var existingCourseIds = _context.Subjects
            .Where(s => s.CourseId != null)
            .Select(s => s.CourseId!.Value)
            .ToHashSet();

        int added = 0;
        foreach (var c in courses.Where(c => !string.IsNullOrWhiteSpace(c.Name)))
        {
            if (c.CourseId > 0 && existingCourseIds.Contains(c.CourseId)) continue;
            if (existingNames.Contains(c.Name.ToLower())) continue;

            _context.Subjects.Add(new Subject
            {
                Name = c.Name.Trim(),
                Credits = c.Credits > 0 ? c.Credits : 3,
                Semester = c.Semester > 0 ? c.Semester : 1,
                HasLab = c.HasLab,
                ConfidenceLevel = 5,
                CourseId = c.CourseId > 0 ? c.CourseId : null,
                CourseUrl = string.IsNullOrWhiteSpace(c.CourseUrl) ? null : c.CourseUrl,
                AcademicYear = string.IsNullOrWhiteSpace(c.AcademicYear) ? null : c.AcademicYear,
                SemesterType = string.IsNullOrWhiteSpace(c.SemesterType) ? null : c.SemesterType,
            });
            added++;
        }
        _context.SaveChanges();
        return added;
    }
}