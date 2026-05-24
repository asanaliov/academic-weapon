using academic_weapon.Data;
using academic_weapon.Models;
using Microsoft.AspNetCore.Mvc;

namespace academic_weapon.Controllers;

public class StudySessionsController : Controller
{
    private readonly AppDbContext _context;

    public StudySessionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult Create(StudySession session)
    {
        _context.StudySessions.Add(session);
        _context.SaveChanges();
        return RedirectToAction("Details", "Subjects", new { id = session.SubjectId });
    }

    [HttpPost]
    public IActionResult ToggleComplete(int id, int subjectId)
    {
        var session = _context.StudySessions.Find(id);
        if (session != null)
        {
            session.IsCompleted = !session.IsCompleted;
            _context.SaveChanges();
        }
        return RedirectToAction("Details", "Subjects", new { id = subjectId });
    }

    [HttpPost]
    public IActionResult Delete(int id, int subjectId)
    {
        var session = _context.StudySessions.Find(id);
        if (session != null)
        {
            _context.StudySessions.Remove(session);
            _context.SaveChanges();
        }
        return RedirectToAction("Details", "Subjects", new { id = subjectId });
    }
}