namespace academic_weapon.Models;

public class Material
{
    public string Id { get; set; }
    public string SubjectId { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public string? Content { get; set; }
    public string? FilePath { get; set; }
    public int? ConfidenceLevel { get; set; }
    
}