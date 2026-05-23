using academic_weapon.Models;

namespace academic_weapon.ViewModels;

public class DeadlineItem
{
    public Assignment Assignment { get; set; }
    public string SubjectName { get; set; }
}

public class SessionItem
{
    public StudySession Session { get; set; }
    public string SubjectName { get; set; }
}

public class HomeViewModel
{
    public List<Subject> Subjects { get; set; }
    public double? WeightedGpa { get; set; }
    public int TotalCredits { get; set; }
    public int CompletedCredits { get; set; }
    public List<DeadlineItem> UpcomingDeadlines { get; set; }
    public List<SessionItem> UpcomingSessions { get; set; }
}