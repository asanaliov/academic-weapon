namespace academic_weapon.Models;

public class ImportedCourse
{
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
    public int Credits { get; set; }
    public int Semester { get; set; }
    public bool HasLab { get; set; }

    // FINKI Moodle extras
    public int CourseId { get; set; }
    public string CourseUrl { get; set; } = "";
    public string AcademicYear { get; set; } = "";
    public string SemesterType { get; set; } = "";
}
