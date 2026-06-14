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

    // Points already earned toward the final 0–100 grade: sum(weight × score) / 100.
    public double CurrentPoints => GradingComponents
        .Where(c => c.Score.HasValue)
        .Sum(c => c.Weight * c.Score!.Value) / 100;

    public double RemainingWeight => TotalWeight - WeightCovered;

    // Average % needed on the remaining (unscored) weight to finish at targetPercent overall.
    // null when there is nothing left to score; ≤0 means already secured; >100 means out of reach.
    public double? NeededForTarget(double targetPercent) =>
        RemainingWeight <= 0 ? null : (targetPercent - CurrentPoints) / RemainingWeight * 100;
}