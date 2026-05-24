namespace academic_weapon.Models;

public class Subject
{
    public int Id { get; set; }
    public string Name {get; set; }
    public int Credits {get; set; }
    public int Semester {get; set; }
    public bool IsCompleted { get; set; }
    public bool HasLab { get; set; }
    public int ConfidenceLevel { get; set; }
    public double? FinalGrade { get; set; }

    // FINKI Moodle metadata (populated by the importer; null for manually-added subjects)
    public int? CourseId { get; set; }
    public string? CourseUrl { get; set; }
    public string? AcademicYear { get; set; }
    public string? SemesterType { get; set; }
}