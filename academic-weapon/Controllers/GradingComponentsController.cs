using academic_weapon.Data;
using academic_weapon.Models;
using Microsoft.AspNetCore.Mvc;

namespace academic_weapon.Controllers;

public class GradingComponentsController : Controller
{
    private readonly AppDbContext _context;

    public GradingComponentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult Create(GradingComponent component)
    {
        if (ModelState.IsValid)
        {
            _context.GradingComponents.Add(component);
            _context.SaveChanges();
        }
        return RedirectToAction("Details", "Subjects", new { id = component.SubjectId });
    }

    [HttpPost]
    public IActionResult UpdateScore(int id, double? score, int subjectId)
    {
        var component = _context.GradingComponents.Find(id);
        if (component != null)
        {
            component.Score = score;
            _context.SaveChanges();
        }
        return RedirectToAction("Details", "Subjects", new { id = subjectId });
    }

    [HttpPost]
    public IActionResult Delete(int id, int subjectId)
    {
        var component = _context.GradingComponents.Find(id);
        if (component != null)
        {
            _context.GradingComponents.Remove(component);
            _context.SaveChanges();
        }
        return RedirectToAction("Details", "Subjects", new { id = subjectId });
    }
}