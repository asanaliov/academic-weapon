using academic_weapon.Data;
using academic_weapon.Models;
using Microsoft.AspNetCore.Mvc;

namespace academic_weapon.Controllers;

public class NotesController : Controller
{
    private readonly AppDbContext _context;

    public NotesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult Create(Note note)
    {
        note.CreatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(note.Content))
        {
            _context.Notes.Add(note);
            _context.SaveChanges();
        }
        return RedirectToAction("Details", "Subjects", new { id = note.SubjectId });
    }

    [HttpPost]
    public IActionResult Delete(int id, int subjectId)
    {
        var note = _context.Notes.Find(id);
        if (note != null)
        {
            _context.Notes.Remove(note);
            _context.SaveChanges();
        }
        return RedirectToAction("Details", "Subjects", new { id = subjectId });
    }
}