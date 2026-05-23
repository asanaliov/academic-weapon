# Features

## Implemented

### Dashboard (`/`)
- Weighted GPA card (percentage, based on completed subjects)
- Credits completed / total card
- Subjects count (active vs done) card
- Upcoming deadlines panel — next 8 non-completed assignments, sorted by due date, overdue highlighted red
- Upcoming study sessions panel — next 8 scheduled sessions
- All subjects listed by semester, clickable to Details

### Subjects (`/Subjects`)
- Full CRUD — list, create, edit, delete
- Fields: Name, Credits, Semester, ConfidenceLevel (0–10), HasLab, IsCompleted, FinalGrade
- Grouped by semester on both Index and Dashboard
- Delete cascades to all child records

### Subject Details (`/Subjects/Details/{id}`)

**Grading Components**
- Define breakdown: e.g. Midterm 40%, Final 60%
- Enter score per component (0–100) — auto-saves on change
- Calculated grade shown live (weighted average of scored components)
- Progress bar showing % of grade weight covered

**Assignments & Deadlines**
- Add by title, type (Homework/Exam/Quiz/Project/Lab), due date
- Mark complete / undo
- Overdue shown in red, due within 2 days in gold

**Materials**
- Add by title, type (Note/Slides/Book/Video/Other), optional description/link
- Per-material confidence level (0–10) selector

**Study Sessions**
- Schedule a session with date and duration (minutes)
- Optional notes
- Mark complete / undo

**Notes**
- Free-text notes with timestamp
- Delete per note

## Planned / Ideas

- [ ] Letter grade conversion (A/B/C/D/F mapping from percentage)
- [ ] Export to PDF or CSV
- [ ] Semester-level GPA breakdown (not just cumulative)
- [ ] Subject tags / categories (STEM, humanities, elective)
- [ ] Repeat study sessions (weekly recurrence)
- [ ] File attachment for materials (upload to wwwroot/uploads)
- [ ] Dark/light theme toggle