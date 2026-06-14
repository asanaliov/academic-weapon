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

public class SemesterSummary
{
    public int Semester { get; set; }
    public double? Gpa { get; set; }
    public int CompletedCredits { get; set; }
    public int TotalCredits { get; set; }
    public int SubjectCount { get; set; }
    public int CompletedCount { get; set; }
}

public class HomeViewModel
{
    public List<Subject> Subjects { get; set; }
    public double? WeightedGpa { get; set; }
    public int TotalCredits { get; set; }
    public int CompletedCredits { get; set; }
    public List<DeadlineItem> UpcomingDeadlines { get; set; }
    public List<SessionItem> UpcomingSessions { get; set; }
    public List<SemesterSummary> SemesterSummaries { get; set; } = [];
}

// ── Agenda ──────────────────────────────────────────────────────────────

public class AgendaItem
{
    public DateTime Date { get; set; }
    public bool IsSession { get; set; }          // false = assignment deadline
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public string Title { get; set; } = "";      // assignment title or "Study session"
    public string Detail { get; set; } = "";     // type badge / duration
    public bool IsCompleted { get; set; }
}

public class AgendaViewModel
{
    public List<AgendaItem> Items { get; set; } = [];
}
