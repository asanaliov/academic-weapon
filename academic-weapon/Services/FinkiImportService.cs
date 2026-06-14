using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using academic_weapon.Models;
using HtmlAgilityPack;

namespace academic_weapon.Services;

public class FinkiImportResult
{
    public bool Success { get; set; }
    public string Error { get; set; } = "";
    public List<ImportedCourse> Courses { get; set; } = [];
    public string DebugHtmlSnippet { get; set; } = "";
}

/// <summary>
/// Imports a student's enrolled courses from the FINKI Moodle (courses.finki.ukim.mk).
///
/// Flow: CAS login (cas.finki.ukim.mk) → extract Moodle sesskey → call Moodle's own
/// AJAX endpoint (core_course_get_enrolled_courses_by_timeline_classification), which
/// returns the user's *enrolled* courses as JSON. HTML scraping is kept only as a
/// fallback because the frontpage lists every FINKI course, not just the student's.
/// </summary>
public class FinkiImportService
{
    public async Task<FinkiImportResult> ImportAsync(string username, string password, string coursesUrl)
    {
        var jar = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = jar,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 15,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0 Safari/537.36");
        client.Timeout = TimeSpan.FromSeconds(40);

        if (string.IsNullOrWhiteSpace(coursesUrl)) coursesUrl = "https://courses.finki.ukim.mk/";
        var wwwroot = GetWwwroot(coursesUrl);

        try
        {
            // Step 1: hit Moodle's login page — it redirects to the CAS login form.
            var landing = await client.GetAsync($"{wwwroot}/login/index.php");
            var landingHtml = await landing.Content.ReadAsStringAsync();
            var landingUrl = landing.RequestMessage?.RequestUri?.ToString() ?? "";

            if (!IsLoggedIn(landingHtml))
            {
                if (!LooksLikeLoginForm(landingHtml))
                    return Fail("Could not reach the CAS login form. FINKI may be down or the URL has changed.",
                        landingHtml);

                // Step 2: submit credentials to CAS, carrying over its hidden state
                // fields (lt, execution, _eventId). Checkboxes like "warn" must stay
                // unchecked or CAS interrupts every redirect with a warning page.
                var (postUrl, fields) = BuildLoginPost(landingHtml, landingUrl, username, password);
                var loginResp = await client.PostAsync(postUrl, new FormUrlEncodedContent(fields));
                var loginHtml = await loginResp.Content.ReadAsStringAsync();

                // Step 3: verify the session, never by keyword-matching the page text.
                // (Moodle pages legitimately contain words like "invalid".)
                if (!IsLoggedIn(loginHtml))
                {
                    // Still on the CAS form → credentials rejected. Surface CAS's own message.
                    if (LooksLikeLoginForm(loginHtml))
                        return Fail(ExtractCasError(loginHtml)
                                    ?? "CAS rejected the login — check your FINKI username and password.", loginHtml);

                    // Some flows land on a page without user markers; confirm against the dashboard.
                    var check = await client.GetStringAsync($"{wwwroot}/my/");
                    if (!IsLoggedIn(check))
                        return Fail("Login did not produce a Moodle session. Check your credentials and try again.",
                            check);
                    loginHtml = check;
                }

                // Step 4: preferred path — ask Moodle itself for the enrolled courses.
                var sesskey = ExtractSesskey(loginHtml) ?? ExtractSesskey(await client.GetStringAsync($"{wwwroot}/my/"));
                if (sesskey != null)
                {
                    var apiCourses = await FetchEnrolledCoursesViaAjax(client, wwwroot, sesskey);
                    if (apiCourses is { Count: > 0 })
                        return new FinkiImportResult { Success = true, Courses = apiCourses };
                }
            }

            // Fallback: scrape whatever page the user pointed us at.
            var coursesHtml = await client.GetStringAsync(coursesUrl);
            return ParseCourses(coursesHtml);
        }
        catch (TaskCanceledException)
        {
            return Fail("Request timed out. FINKI servers may be slow — try again in a minute.");
        }
        catch (HttpRequestException ex)
        {
            return Fail($"Could not reach FINKI: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Fail($"Unexpected error: {ex.Message}");
        }
    }

    // ── Session detection ───────────────────────────────────────────────

    // Every Moodle page embeds M.cfg with the current user id; 0 means guest.
    private static bool IsLoggedIn(string html)
    {
        var m = Regex.Match(html, @"""userId""\s*:\s*(\d+)");
        if (m.Success) return m.Groups[1].Value != "0";
        // CAS pages have no M.cfg at all — treat "has logout link" as a secondary signal.
        return html.Contains("login/logout.php");
    }

    private static bool LooksLikeLoginForm(string html) =>
        Regex.IsMatch(html, @"<input[^>]+type\s*=\s*[""']password[""']", RegexOptions.IgnoreCase);

    private static string? ExtractSesskey(string html)
    {
        var m = Regex.Match(html, @"""sesskey""\s*:\s*""([^""]+)""");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? ExtractCasError(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var node = doc.DocumentNode.SelectSingleNode(
            "//div[@id='msg' and contains(@class,'errors')]|//div[contains(@class,'alert-danger')]|//*[@id='status' and contains(@class,'errors')]");
        var text = node == null ? null : HtmlEntity.DeEntitize(node.InnerText).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : Regex.Replace(text, @"\s+", " ");
    }

    // ── CAS form submission ─────────────────────────────────────────────

    private static (string postUrl, Dictionary<string, string> fields) BuildLoginPost(
        string html, string pageUrl, string username, string password)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var form = doc.DocumentNode.SelectSingleNode("//form[.//input[@type='password']]")
                   ?? doc.DocumentNode.SelectSingleNode("//form");

        var action = form?.GetAttributeValue("action", "") ?? "";
        var postUrl = BuildAbsoluteUrl(pageUrl, HtmlEntity.DeEntitize(action));

        var fields = new Dictionary<string, string>();
        foreach (var input in form?.SelectNodes(".//input") ?? Enumerable.Empty<HtmlNode>())
        {
            var name = input.GetAttributeValue("name", "");
            var type = input.GetAttributeValue("type", "text").ToLowerInvariant();
            var value = HtmlEntity.DeEntitize(input.GetAttributeValue("value", ""));
            if (string.IsNullOrEmpty(name)) continue;

            switch (type)
            {
                case "hidden":
                case "submit":
                    fields[name] = value;
                    break;
                case "password":
                    fields[name] = password;
                    break;
                case "text":
                case "email":
                    fields[name] = username;
                    break;
                // checkbox ("warn") and reset inputs are intentionally not sent
            }
        }

        fields.TryAdd("username", username);
        fields.TryAdd("password", password);
        return (postUrl, fields);
    }

    // ── Moodle AJAX API (preferred course source) ───────────────────────

    private static async Task<List<ImportedCourse>?> FetchEnrolledCoursesViaAjax(
        HttpClient client, string wwwroot, string sesskey)
    {
        const string method = "core_course_get_enrolled_courses_by_timeline_classification";
        var url = $"{wwwroot}/lib/ajax/service.php?sesskey={Uri.EscapeDataString(sesskey)}&info={method}";
        var payload = $@"[{{""index"":0,""methodname"":""{method}"",""args"":{{""offset"":0,""limit"":0,""classification"":""all"",""sort"":""fullname""}}}}]";

        try
        {
            var resp = await client.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"));
            var body = await resp.Content.ReadAsStringAsync();

            using var json = JsonDocument.Parse(body);
            var first = json.RootElement[0];
            if (first.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.True)
                return null;

            var courses = new List<ImportedCourse>();
            foreach (var c in first.GetProperty("data").GetProperty("courses").EnumerateArray())
            {
                var fullName = c.TryGetProperty("fullname", out var fn) ? WebUtility.HtmlDecode(fn.GetString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(fullName)) continue;

                var (cleanName, academicYear, semesterType) = ParseFinkiCourseName(fullName);
                if (cleanName.Length < 2) continue;

                var id = c.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
                if (courses.Any(x => x.CourseId == id ||
                                     x.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase))) continue;

                courses.Add(new ImportedCourse
                {
                    Name = cleanName,
                    FullName = fullName,
                    CourseId = id,
                    CourseUrl = c.TryGetProperty("viewurl", out var vu) ? vu.GetString() ?? "" : "",
                    AcademicYear = academicYear,
                    SemesterType = semesterType,
                    // Z (winter) → odd semester, L (summer) → even; adjustable later in Edit.
                    Semester = semesterType.Equals("L", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
                    Credits = 0,
                });
            }
            return courses;
        }
        catch
        {
            return null; // fall through to HTML scraping
        }
    }

    // ── HTML scraping fallback ──────────────────────────────────────────

    private FinkiImportResult ParseCourses(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var courses = new List<ImportedCourse>();

        // Strategy 0: FINKI Moodle frontpage — <div class="coursebox" data-courseid="...">
        var courseBoxes = doc.DocumentNode
            .SelectNodes("//div[contains(concat(' ',normalize-space(@class),' '),' coursebox ') and @data-courseid]")
            ?? Enumerable.Empty<HtmlNode>();

        foreach (var box in courseBoxes)
        {
            var link = box.SelectSingleNode(".//h3[contains(@class,'coursename')]//a")
                       ?? box.SelectSingleNode(".//a[contains(@href,'course/view.php')]");
            if (link == null) continue;

            var fullName = HtmlEntity.DeEntitize(link.InnerText.Trim());
            if (string.IsNullOrWhiteSpace(fullName)) continue;

            var courseId = ParseInt(box.GetAttributeValue("data-courseid", "0"));
            var (cleanName, academicYear, semesterType) = ParseFinkiCourseName(fullName);
            if (cleanName.Length < 2) continue;
            if (courses.Any(c => c.CourseId == courseId || c.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase))) continue;

            courses.Add(new ImportedCourse
            {
                Name = cleanName,
                FullName = fullName,
                CourseId = courseId,
                CourseUrl = link.GetAttributeValue("href", ""),
                AcademicYear = academicYear,
                SemesterType = semesterType,
                Semester = semesterType.Equals("L", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
                Credits = 0,
            });
        }

        // Strategy 1: sidebar/nav links whose title holds the full course name
        if (!courses.Any())
        {
            var finkiLinks = doc.DocumentNode
                .SelectNodes("//a[@href and contains(@href,'course/view.php') and @title]")
                ?? Enumerable.Empty<HtmlNode>();

            foreach (var a in finkiLinks)
            {
                var raw = HtmlEntity.DeEntitize(a.GetAttributeValue("title", "").Trim());
                if (string.IsNullOrWhiteSpace(raw)) continue;

                var (cleanName, academicYear, semesterType) = ParseFinkiCourseName(raw);
                if (cleanName.Length < 3) continue;
                if (courses.Any(c => c.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase))) continue;

                courses.Add(new ImportedCourse
                {
                    Name = cleanName,
                    FullName = raw,
                    CourseId = ParseCourseIdFromUrl(a.GetAttributeValue("href", "")),
                    CourseUrl = a.GetAttributeValue("href", ""),
                    AcademicYear = academicYear,
                    SemesterType = semesterType,
                    Semester = semesterType.Equals("L", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
                    Credits = 0,
                });
            }
        }

        // Strategy 2: any anchor pointing at course/view.php (text content as name)
        if (!courses.Any())
        {
            var links = doc.DocumentNode.SelectNodes("//a[contains(@href,'course/view')]")
                        ?? Enumerable.Empty<HtmlNode>();
            foreach (var a in links)
            {
                var raw = HtmlEntity.DeEntitize(a.InnerText.Trim());
                if (raw.Length < 4 || raw.Length > 120) continue;
                if (Regex.IsMatch(raw, @"^\d|^(No events|Mon|Tue|Wed|Thu|Fri|Sat|Sun)", RegexOptions.IgnoreCase)) continue;

                var (cleanName, academicYear, semesterType) = ParseFinkiCourseName(raw);
                if (courses.Any(c => c.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase))) continue;

                courses.Add(new ImportedCourse
                {
                    Name = cleanName,
                    FullName = raw,
                    CourseId = ParseCourseIdFromUrl(a.GetAttributeValue("href", "")),
                    CourseUrl = a.GetAttributeValue("href", ""),
                    AcademicYear = academicYear,
                    SemesterType = semesterType,
                    Semester = semesterType.Equals("L", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
                    Credits = 0,
                });
            }
        }

        // Strategy 3: any table with a recognisable course-name column
        if (!courses.Any())
        {
            foreach (var table in doc.DocumentNode.SelectNodes("//table") ?? Enumerable.Empty<HtmlNode>())
            {
                var headers = (table.SelectNodes(".//th") ?? Enumerable.Empty<HtmlNode>())
                    .Select(h => h.InnerText.Trim().ToLower())
                    .ToList();

                int nameIdx   = FindIndex(headers, "name", "naziv", "предмет", "course", "subject");
                int creditIdx = FindIndex(headers, "credit", "ects", "кредит");
                int semIdx    = FindIndex(headers, "sem");
                if (nameIdx < 0) continue;

                foreach (var row in table.SelectNodes(".//tbody/tr") ?? Enumerable.Empty<HtmlNode>())
                {
                    var cells = (row.SelectNodes(".//td") ?? Enumerable.Empty<HtmlNode>())
                        .Select(c => c.InnerText.Trim())
                        .ToList();
                    if (cells.Count == 0 || nameIdx >= cells.Count) continue;

                    var name = HtmlEntity.DeEntitize(cells[nameIdx]);
                    if (string.IsNullOrWhiteSpace(name) || name.Length < 3) continue;

                    courses.Add(new ImportedCourse
                    {
                        Name     = name,
                        Credits  = creditIdx >= 0 && creditIdx < cells.Count ? ParseInt(cells[creditIdx]) : 0,
                        Semester = semIdx    >= 0 && semIdx    < cells.Count ? ParseInt(cells[semIdx])    : 1,
                    });
                }

                if (courses.Any()) break;
            }
        }

        if (!courses.Any())
            return Fail(
                "Logged in, but no courses were found on the page. Try pasting the URL of your dashboard (https://courses.finki.ukim.mk/my/) instead.",
                html);

        return new FinkiImportResult { Success = true, Courses = courses };
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    // Splits "Агентно-базирани системи-2025/2026/L" into ("Агентно-базирани системи", "2025/2026", "L").
    private static (string cleanName, string academicYear, string semesterType) ParseFinkiCourseName(string raw)
    {
        var trimmed = raw.Trim();
        var match = Regex.Match(trimmed, @"^(?<name>.+?)\s*-\s*(?<year>\d{4}/\d{4})(?:/(?<sem>[A-Za-zЗзЛл]+))?\s*$");
        if (!match.Success)
            return (trimmed, "", "");

        return (match.Groups["name"].Value.Trim(),
                match.Groups["year"].Value,
                match.Groups["sem"].Success ? match.Groups["sem"].Value.ToUpperInvariant() : "");
    }

    private static string GetWwwroot(string url)
    {
        try
        {
            var uri = new Uri(url);
            return $"{uri.Scheme}://{uri.Host}";
        }
        catch
        {
            return "https://courses.finki.ukim.mk";
        }
    }

    private static int ParseCourseIdFromUrl(string url)
    {
        var m = Regex.Match(url ?? "", @"[?&]id=(\d+)");
        return m.Success ? ParseInt(m.Groups[1].Value) : 0;
    }

    private static int FindIndex(List<string> headers, params string[] keywords)
    {
        for (int i = 0; i < headers.Count; i++)
            if (keywords.Any(k => headers[i].Contains(k)))
                return i;
        return -1;
    }

    private static int ParseInt(string s) =>
        int.TryParse(new string(s.Where(char.IsDigit).ToArray()), out var n) ? n : 0;

    private static string BuildAbsoluteUrl(string baseUrl, string action)
    {
        if (string.IsNullOrEmpty(action)) return baseUrl;
        if (action.StartsWith("http")) return action;
        var uri = new Uri(baseUrl);
        return action.StartsWith("/")
            ? $"{uri.Scheme}://{uri.Host}{action}"
            : new Uri(uri, action).ToString();
    }

    private static FinkiImportResult Fail(string msg, string html = "") =>
        new() { Success = false, Error = msg, DebugHtmlSnippet = html.Length > 2000 ? html[..2000] : html };
}
