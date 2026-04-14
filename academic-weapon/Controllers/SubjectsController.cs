using academic_weapon.Data;
using academic_weapon.Models;
using Microsoft.AspNetCore.Mvc;

namespace academic_weapon.Controllers;

public class SubjectsController : Controller
{
    private AppDbContext _context;
    
    public SubjectsController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View(_context.Subjects.ToList());
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Subject subject)
    {
        _context.Subjects.Add(subject);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    public IActionResult Details(int id)
    {
        Subject? subject = _context.Subjects.Find(id);
        if (subject == null)
        {
            return NotFound();
        }
        return View(subject);
    }
    
}