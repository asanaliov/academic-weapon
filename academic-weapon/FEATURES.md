# Features

## Implemented

### Dashboard (`/`)
- Weighted average (percentage + FINKI 5–10 grade equivalent, based on completed subjects)
- Credits completed / total, active vs finished subject counts
- Per-semester breakdown chips (semester GPA, credits, completion)
- Upcoming deadlines panel — next 8 non-completed assignments, overdue highlighted
- Upcoming study sessions panel — next 8 scheduled sessions
- All subjects listed by semester (ledger style), clickable to Details

### Agenda (`/Home/Agenda`)
- Every deadline + study session across all subjects, chronological
- Grouped by day (Today / Tomorrow / weekday), separate Overdue section
- Quick ✓ complete / ↩ undo inline (redirects back to the agenda)

### Subjects (`/Subjects`)
- Full CRUD — list, create, edit, delete
- Search by name (`?q=`)
- CSV export (`/Subjects/Export`) — includes percentage and FINKI grade
- Fields: Name, Credits, Semester, ConfidenceLevel (0–10), HasLab, IsCompleted, FinalGrade
- FINKI Moodle metadata preserved through Edit (CourseId/CourseUrl/AcademicYear/SemesterType)
- Grouped by semester on both Index and Dashboard
- Delete cascades to all child records

### Subject Details (`/Subjects/Details/{id}`)

**Grading Components**
- Define breakdown: e.g. Midterm 40%, Final 60%
- Enter score per component (0–100) — auto-saves on change
- Calculated grade shown live (weighted average of scored components)
- Progress bar showing % of grade weight covered
- **Grade target calculator** — "to finish with grade 6/7/8/9/10 you need avg X% on the rest"
  (`SubjectDetailsViewModel.NeededForTarget`, thresholds in `Helpers/GradeHelper`)

**Assignments & Deadlines** — title, type, due date; complete/undo; overdue/due-soon coloring

**Materials** — title, type, optional description or link (links render as "open ↗"); per-material confidence

**Study Sessions** — date + duration + notes; complete/undo

**Notes** — free-text with timestamp; delete per note

### FINKI import (`/Import`)
- CAS login → Moodle session → `core_course_get_enrolled_courses_by_timeline_classification`
  AJAX call returns the student's *enrolled* courses as JSON (HTML scraping kept as fallback)
- Login success detected via Moodle's `M.cfg` userId marker (never keyword matching)
- CAS rejection surfaces CAS's own error message
- Manual JSON paste fallback for the password-averse

### Exam Autopsy (`/Exams`) — community archive
- Browse/filter by course, professor, year (public); upload/rate/edit need login
- Difficulty ratings (1–5, averaged), uploader-only edit/delete

### Design — "The Quiet Library"
- Warm ink-black + paper-cream + brass accent; Fraunces / Schibsted Grotesk / Spline Sans Mono
- Hairline panels, ledger rows, staggered page-load reveals, grain + glow atmosphere
- Global flash messages (TempData Success/Error/ImportResult) in `_Layout`
- Empty states with CTAs everywhere; active nav states

## Planned / Ideas

- [ ] Subject tags / categories (STEM, humanities, elective)
- [ ] Repeat study sessions (weekly recurrence)
- [ ] File attachment for materials (upload to wwwroot/uploads)
- [ ] Dark/light theme toggle
- [ ] PDF export
