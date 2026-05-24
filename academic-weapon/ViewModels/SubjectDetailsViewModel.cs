using academic_weapon.Models;

namespace academic_weapon.ViewModels;

public class SubjectDetailsViewModel
{
    public Subject Subject { get; set; }
    public List<GradingComponent> GradingComponents { get; set; }
    public List<Note> Notes { get; set; }
    public List<Material> Materials { get; set; }
    public List<Assignment> Assignments { get; set; }
    public List<StudySession> StudySessions { get; set; }
    public double? CalculatedGrade { get; set; }
    public double WeightCovered { get; set; }
    public double TotalWeight { get; set; }
    public double ProgressPercent => TotalWeight > 0 ? WeightCovered / TotalWeight * 100 : 0;
}