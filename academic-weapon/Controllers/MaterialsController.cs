using academic_weapon.Data;
using academic_weapon.Models;
using Microsoft.AspNetCore.Mvc;

namespace academic_weapon.Controllers;

public class MaterialsController : Controller
{
    private readonly AppDbContext _context;

    public MaterialsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult Create(Material material)
    {
        if (!string.IsNullOrWhiteSpace(material.Title))
        {
            _context.Materials.Add(material);
            _context.SaveChanges();
        }
        return RedirectToAction("Details", "Subjects", new { id = material.SubjectId });
    }

    [HttpPost]
    public IActionResult UpdateConfidence(int id, int confidence, int subjectId)
    {
        var material = _context.Materials.Find(id);
        if (material != null)
        {
            material.ConfidenceLevel = confidence;
            _context.SaveChanges();
        }
        return RedirectToAction("Details", "Subjects", new { id = subjectId });
    }

    [HttpPost]
    public IActionResult Delete(int id, int subjectId)
    {
        var material = _context.Materials.Find(id);
        if (material != null)
        {
            _context.Materials.Remove(material);
            _context.SaveChanges();
        }
        return RedirectToAction("Details", "Subjects", new { id = subjectId });
    }
}