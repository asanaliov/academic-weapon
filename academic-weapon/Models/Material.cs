namespace academic_weapon.Models;

public class Material
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public string? Content { get; set; }
    public string? FilePath { get; set; }
    public int? ConfidenceLevel { get; set; }
    
}