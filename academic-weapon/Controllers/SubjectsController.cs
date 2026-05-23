using academic_weapon.Data;
using academic_weapon.Models;
using academic_weapon.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace academic_weapon.Controllers;

public class SubjectsController : Controller
{
    private readonly AppDbContext _context;

    public SubjectsController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View(_context.Subjects.OrderBy(s => s.Semester).ThenBy(s => s.Name).ToList());
    }

    public IActionResult Create() => View();

    [HttpPost]
    public IActionResult Create(Subject subject)
    {
        if (!ModelState.IsValid) return View(subject);
        _context.Subjects.Add(subject);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    public IActionResult Details(int id)
    {
        var subject = _context.Subjects.Find(id);
        if (subject == null) return NotFound();

        var components = _context.GradingComponents.Where(g => g.SubjectId == id).ToList();
        var totalWeight = components.Sum(c => c.Weight);
        var weightCovered = components.Where(c => c.Score.HasValue).Sum(c => c.Weight);

        double? calculatedGrade = null;
        if (totalWeight > 0 && weightCovered > 0)
            calculatedGrade = components.Where(c => c.Score.HasValue).Sum(c => c.Weight * c.Score!.Value) / totalWeight;

        var vm = new SubjectDetailsViewModel
        {
            Subject = subject,
            GradingComponents = components,
            Notes = _context.Notes.Where(n => n.SubjectId == id).OrderByDescending(n => n.CreatedAt).ToList(),
            Materials = _context.Materials.Where(m => m.SubjectId == id).ToList(),
            Assignments = _context.Assignments.Where(a => a.SubjectId == id).OrderBy(a => a.DueDate).ToList(),
            StudySessions = _context.StudySessions.Where(s => s.SubjectId == id).OrderBy(s => s.PlannedDate).ToList(),
            CalculatedGrade = calculatedGrade,
            WeightCovered = weightCovered,
            TotalWeight = totalWeight
        };
        return View(vm);
    }

    public IActionResult Edit(int id)
    {
        var subject = _context.Subjects.Find(id);
        if (subject == null) return NotFound();
        return View(subject);
    }

    [HttpPost]
    public IActionResult Edit(Subject subject)
    {
        if (!ModelState.IsValid) return View(subject);
        _context.Subjects.Update(subject);
        _context.SaveChanges();
        return RedirectToAction("Details", new { id = subject.Id });
    }

    public IActionResult Delete(int id)
    {
        var subject = _context.Subjects.Find(id);
        if (subject == null) return NotFound();
        return View(subject);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeleteConfirmed(int id)
    {
        var subject = _context.Subjects.Find(id);
        if (subject != null)
        {
            _context.GradingComponents.RemoveRange(_context.GradingComponents.Where(g => g.SubjectId == id));
            _context.Notes.RemoveRange(_context.Notes.Where(n => n.SubjectId == id));
            _context.Materials.RemoveRange(_context.Materials.Where(m => m.SubjectId == id));
            _context.Assignments.RemoveRange(_context.Assignments.Where(a => a.SubjectId == id));
            _context.StudySessions.RemoveRange(_context.StudySessions.Where(s => s.SubjectId == id));
            _context.Subjects.Remove(subject);
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }
}