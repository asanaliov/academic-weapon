namespace academic_weapon.Models;

public class StudySession
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public DateTime PlannedDate { get; set; }
    public int DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }
}