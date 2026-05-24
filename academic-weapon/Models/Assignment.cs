namespace academic_weapon.Models;

public class Assignment
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string Title { get; set; }
    public string Type { get; set; } // Exam, Homework, Quiz, Project, Lab
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public string? Notes { get; set; }
}