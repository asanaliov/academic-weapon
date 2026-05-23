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

        var now = DateTime.UtcNow;
        var upcoming = now.AddDays(14);

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
            UpcomingSessions = sessions
        };

        return View(vm);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}