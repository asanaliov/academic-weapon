namespace academic_weapon.Models;

public class ImportedCourse
{
    public string Name { get; set; } = "";
    public int Credits { get; set; }
    public int Semester { get; set; }
    public bool HasLab { get; set; }
}