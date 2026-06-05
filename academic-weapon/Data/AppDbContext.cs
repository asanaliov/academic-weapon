using academic_weapon.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace academic_weapon.Data;

// Inherits IdentityDbContext so the Identity tables (AspNetUsers, AspNetRoles,
// ...) are created alongside the app's own tables, keyed on ApplicationUser.
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Subject> Subjects { get; set; }
    public DbSet<GradingComponent> GradingComponents { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<StudySession> StudySessions { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<Rating> Ratings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // One rating per user per exam; re-rating updates that row.
        builder.Entity<Rating>()
            .HasIndex(r => new { r.ExamId, r.UserId })
            .IsUnique();

        // Deleting an exam removes its ratings; deleting a user is blocked
        // while they still own exams/ratings (avoid orphaned community content).
        builder.Entity<Rating>()
            .HasOne(r => r.Exam)
            .WithMany(e => e.Ratings)
            .HasForeignKey(r => r.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Rating>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Exam>()
            .HasOne(e => e.UploadedBy)
            .WithMany()
            .HasForeignKey(e => e.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
