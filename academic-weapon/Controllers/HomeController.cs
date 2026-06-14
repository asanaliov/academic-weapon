using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using academic_weapon.Data;
using academic_weapon.Models;
using academic_weapon.ViewModels;

namespace academic_weapon.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var subjects = _context.Subjects.OrderBy(s => s.Semester).ThenBy(s => s.Name).ToList();
        var subjectMap = subjects.ToDictionary(s => s.Id, s => s.Name);

        var completedWithGrade = subjects.Where(s => s.IsCompleted && s.FinalGrade.HasValue && s.Credits > 0).ToList();
        double? weightedGpa = null;
        if (completedWithGrade.Any())
        {
            var totalCredits = completedWithGrade.Sum(s => s.Credits);
            weightedGpa = completedWithGrade.Sum(s => s.FinalGrade!.Value * s.Credits) / totalCredits;
        }

        var semesterSummaries = subjects
            .GroupBy(s => s.Semester)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var graded = g.Where(s => s.IsCompleted && s.FinalGrade.HasValue && s.Credits > 0).ToList();
                return new SemesterSummary
                {
                    Semester = g.Key,
                    Gpa = graded.Any()
                        ? graded.Sum(s => s.FinalGrade!.Value * s.Credits) / graded.Sum(s => s.Credits)
                        : null,
                    CompletedCredits = g.Where(s => s.IsCompleted).Sum(s => s.Credits),
                    TotalCredits = g.Sum(s => s.Credits),
                    SubjectCount = g.Count(),
                    CompletedCount = g.Count(s => s.IsCompleted),
                };
            })
            .ToList();

        var now = DateTime.UtcNow;

        var deadlines = _context.Assignments
            .Where(a => !a.IsCompleted && a.DueDate >= now.AddDays(-1))
            .OrderBy(a => a.DueDate)
            .Take(8)
            .ToList()
            .Where(a => subjectMap.ContainsKey(a.SubjectId))
            .Select(a => new DeadlineItem { Assignment = a, SubjectName = subjectMap[a.SubjectId] })
            .ToList();

        var sessions = _context.StudySessions
            .Where(s => !s.IsCompleted && s.PlannedDate >= now.AddDays(-1))
            .OrderBy(s => s.PlannedDate)
            .Take(8)
            .ToList()
            .Where(s => subjectMap.ContainsKey(s.SubjectId))
            .Select(s => new SessionItem { Session = s, SubjectName = subjectMap[s.SubjectId] })
            .ToList();

        var vm = new HomeViewModel
        {
            Subjects = subjects,
            WeightedGpa = weightedGpa,
            TotalCredits = subjects.Sum(s => s.Credits),
            CompletedCredits = subjects.Where(s => s.IsCompleted).Sum(s => s.Credits),
            UpcomingDeadlines = deadlines,
            UpcomingSessions = sessions,
            SemesterSummaries = semesterSummaries,
        };

        return View(vm);
    }

    // Chronological view of every deadline and study session across all subjects.
    public IActionResult Agenda()
    {
        var subjectMap = _context.Subjects.ToDictionary(s => s.Id, s => s.Name);
        var since = DateTime.UtcNow.Date.AddDays(-30);

        var items = new List<AgendaItem>();

        items.AddRange(_context.Assignments
            .Where(a => a.DueDate >= since)
            .ToList()
            .Where(a => subjectMap.ContainsKey(a.SubjectId))
            .Select(a => new AgendaItem
            {
                Date = a.DueDate,
                IsSession = false,
                Id = a.Id,
                SubjectId = a.SubjectId,
                SubjectName = subjectMap[a.SubjectId],
                Title = a.Title,
                Detail = a.Type,
                IsCompleted = a.IsCompleted,
            }));

        items.AddRange(_context.StudySessions
            .Where(s => s.PlannedDate >= since)
            .ToList()
            .Where(s => subjectMap.ContainsKey(s.SubjectId))
            .Select(s => new AgendaItem
            {
                Date = s.PlannedDate,
                IsSession = true,
                Id = s.Id,
                SubjectId = s.SubjectId,
                SubjectName = subjectMap[s.SubjectId],
                Title = "Study session",
                Detail = $"{s.DurationMinutes} min" + (string.IsNullOrWhiteSpace(s.Notes) ? "" : $" · {s.Notes}"),
                IsCompleted = s.IsCompleted,
            }));

        return View(new AgendaViewModel
        {
            Items = items.OrderBy(i => i.Date).ThenBy(i => i.SubjectName).ToList()
        });
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
