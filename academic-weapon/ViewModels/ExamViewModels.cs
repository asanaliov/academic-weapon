using academic_weapon.Models;

namespace academic_weapon.ViewModels;

// One row in the browse/filter list, with its aggregated difficulty.
public class ExamListItem
{
    public Exam Exam { get; set; } = null!;
    public double? AverageDifficulty { get; set; }
    public int RatingCount { get; set; }
    public string UploaderName { get; set; } = string.Empty;
}

public class ExamIndexViewModel
{
    public List<ExamListItem> Exams { get; set; } = new();

    // Active filters (echoed back into the form).
    public string? Course { get; set; }
    public string? Professor { get; set; }
    public int? Year { get; set; }

    // Distinct values for the filter dropdowns.
    public List<string> CourseOptions { get; set; } = new();
    public List<string> ProfessorOptions { get; set; } = new();
    public List<int> YearOptions { get; set; } = new();
}

public class ExamDetailsViewModel
{
    public Exam Exam { get; set; } = null!;
    public string UploaderName { get; set; } = string.Empty;
    public double? AverageDifficulty { get; set; }
    public int RatingCount { get; set; }

    // The signed-in user's own rating (null if they haven't rated / aren't logged in).
    public int? UserRating { get; set; }

    // Whether the current user owns this exam (controls Edit/Delete visibility).
    public bool CanManage { get; set; }
}
