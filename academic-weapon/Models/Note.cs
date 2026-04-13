namespace academic_weapon.Models;

public class Note
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}