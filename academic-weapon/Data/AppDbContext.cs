using academic_weapon.Models;
using Microsoft.EntityFrameworkCore;
namespace academic_weapon.Data;


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<GradingComponent> GradingComponents { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<Note> Notes { get; set; }
}