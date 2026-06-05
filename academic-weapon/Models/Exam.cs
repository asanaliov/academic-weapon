using System.ComponentModel.DataAnnotations;

namespace academic_weapon.Models;

// A past exam uploaded to the community "Exam Autopsy" archive.
//
// NOTE: the course is intentionally a free-text tag (CourseName / optional
// CourseCode) and is NOT a foreign key into the personal `Subject` table.
// Subject rows are owned by an individual student (they carry FinalGrade,
// ConfidenceLevel, IsCompleted), so a shared community exam must never point
// at one user's personal course row. The tag stands on its own.
public class Exam
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string CourseName { get; set; } = string.Empty;

    // Optional FINKI course code (e.g. "F18L3S081"), mirrors the CourseId/
    // AcademicYear metadata that already lives on Subject for course identity.
    [StringLength(50)]
    public string? CourseCode { get; set; }

    [Required, StringLength(150)]
    public string Professor { get; set; } = string.Empty;

    [Range(2000, 2100)]
    public int Year { get; set; }

    // Midterm / Final / Makeup / Quiz, etc.
    [Required, StringLength(50)]
    public string ExamType { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    // Stored file metadata. FilePath is web-relative (e.g. /uploads/exams/<guid>.pdf).
    public string FilePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    // Owner (uploader). FK to ApplicationUser.Id (string GUID from Identity).
    public string UploadedById { get; set; } = string.Empty;
    public ApplicationUser? UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}
