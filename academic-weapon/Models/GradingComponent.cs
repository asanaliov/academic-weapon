namespace academic_weapon.Models;

public class GradingComponent
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string Name { get; set; }
    public double Weight { get; set; }
    public double? Score { get; set; }
}