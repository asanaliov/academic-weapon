using academic_weapon.Data;
using academic_weapon.Models;
using Microsoft.AspNetCore.Mvc;

namespace academic_weapon.Controllers;

public class AssignmentsController : Controller
{
    private readonly AppDbContext _context;

    public AssignmentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult Create(Assignment assignment)
    {
        if (!string.IsNullOrWhiteSpace(assignment.Title))
        {
            _context.Assignments.Add(assignment);
            _context.SaveChanges();
        }
        return RedirectToAction("Details", "Subjects", new { id = assignment.SubjectId });
    }

    [HttpPost]
    public IActionResult ToggleComplete(int id, int subjectId)
    {
        var assignment = _context.Assignments.Find(id);
        if (assignment != null)
        {
            assignment.IsCompleted = !assignment.IsCompleted;
            _context.SaveChanges();
        }
        return RedirectToAction("Details", "Subjects", new { id = subjectId });
    }

    [HttpPost]
    public IActionResult Delete(int id, int subjectId)
    {
        var assignment = _context.Assignments.Find(id);
        if (assignment != null)
        {
            _context.Assignments.Remove(assignment);
            _context.SaveChanges();
        }
        return RedirectToAction("Details", "Subjects", new { id = subjectId });
    }
}