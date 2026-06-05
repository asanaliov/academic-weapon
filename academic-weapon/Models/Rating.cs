using System.ComponentModel.DataAnnotations;

namespace academic_weapon.Models;

// A single student's difficulty rating for an exam (1 = easy ... 5 = brutal).
// The Details page averages these across all students, wiki-style.
// A unique index on (ExamId, UserId) enforces one vote per user per exam;
// re-rating updates the existing row.
public class Rating
{
    public int Id { get; set; }

    public int ExamId { get; set; }
    public Exam? Exam { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Range(1, 5)]
    public int Score { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
