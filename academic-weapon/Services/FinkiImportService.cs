using System.Net;
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
        client.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            // Step 1: hit the courses URL — it should redirect us to CAS login
            var initial = await client.GetAsync(coursesUrl);
            var initialHtml = await initial.Content.ReadAsStringAsync();
            var currentUrl = initial.RequestMessage?.RequestUri?.ToString() ?? coursesUrl;

            // If we're already on the courses page (no CAS redirect), skip login
            bool onLoginPage = currentUrl.Contains("login") || currentUrl.Contains("cas") ||
                               initialHtml.Contains("cas") || initialHtml.Contains("password") &&
                               initialHtml.Contains("username");

            if (onLoginPage)
            {
                // Step 2: parse the CAS login form
                var doc = new HtmlDocument();
                doc.LoadHtml(initialHtml);

                var form = doc.DocumentNode.SelectSingleNode("//form");
                if (form == null)
                    return Fail("Could not find the login form on the CAS page. The URL may have changed.");

                // Get form action (absolute URL)
                var action = form.GetAttributeValue("action", "");
                var loginPostUrl = BuildAbsoluteUrl(currentUrl, action);

                // Collect all hidden inputs
                var fields = new Dictionary<string, string>();
                foreach (var input in form.SelectNodes(".//input") ?? Enumerable.Empty<HtmlNode>())
                {
                    var inputName  = input.GetAttributeValue("name", "");
                    var inputType  = input.GetAttributeValue("type", "text").ToLower();
                    var inputValue = input.GetAttributeValue("value", "");

                    if (string.IsNullOrEmpty(inputName)) continue;

                    if (inputType == "hidden")
                        fields[inputName] = inputValue;
                    else if (inputType == "text" || inputName.Contains("user") || inputName.Contains("email"))
                        fields[inputName] = username;
                    else if (inputType == "password")
                        fields[inputName] = password;
                }

                // Ensure username/password are set even if field names are unusual
                if (!fields.ContainsKey("username")) fields["username"] = username;
                if (!fields.ContainsKey("password")) fields["password"] = password;

                // Step 3: submit the login form
                var loginResp = await client.PostAsync(loginPostUrl, new FormUrlEncodedContent(fields));
                var loginHtml = await loginResp.Content.ReadAsStringAsync();

                // Check for login error
                if (loginHtml.Contains("invalid") || loginHtml.Contains("incorrect") ||
                    loginHtml.Contains("failed")  || loginHtml.Contains("погрешна") ||
                    loginHtml.Contains("неточна"))
                    return Fail("Login failed — check your FINKI username and password.");

                // Step 4: re-fetch the courses URL (now authenticated)
                var coursesResp = await client.GetAsync(coursesUrl);
                var coursesHtml = await coursesResp.Content.ReadAsStringAsync();

                return ParseCourses(coursesHtml);
            }
            else
            {
                // Already authenticated (session cookie present), parse directly
                return ParseCourses(initialHtml);
            }
        }
        catch (TaskCanceledException)
        {
            return Fail("Request timed out. Check the URL and try again.");
        }
        catch (Exception ex)
        {
            return Fail($"Unexpected error: {ex.Message}");
        }
    }

    private FinkiImportResult ParseCourses(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var courses = new List<ImportedCourse>();

        // Strategy 0: FINKI Moodle frontpage — <div class="coursebox" data-courseid="...">
        // containing <h3 class="coursename"><a href=".../course/view.php?id=...">Full Name</a></h3>
        var courseBoxes = doc.DocumentNode
            .SelectNodes("//div[contains(concat(' ',normalize-space(@class),' '),' coursebox ') and @data-courseid]")
            ?? Enumerable.Empty<HtmlNode>();

        foreach (var box in courseBoxes)
        {
            var link = box.SelectSingleNode(".//h3[contains(@class,'coursename')]//a");
            if (link == null) continue;

            var fullName = HtmlEntity.DeEntitize(link.InnerText.Trim());
            if (string.IsNullOrWhiteSpace(fullName)) continue;

            var courseId = ParseInt(box.GetAttributeValue("data-courseid", "0"));
            var courseUrl = link.GetAttributeValue("href", "");

            var (cleanName, academicYear, semesterType) = ParseFinkiCourseName(fullName);
            if (cleanName.Length < 2) continue;
            if (courses.Any(c => c.CourseId == courseId || c.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase))) continue;

            courses.Add(new ImportedCourse
            {
                Name = cleanName,
                FullName = fullName,
                CourseId = courseId,
                CourseUrl = courseUrl,
                AcademicYear = academicYear,
                SemesterType = semesterType,
                // Best-effort initial guess: Z (winter) → odd semester, L (summer) → even semester.
                // User can adjust in the Subject edit form; SemesterType preserves the raw value.
                Semester = semesterType.Equals("L", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
                Credits = 0,
            });
        }

        // Strategy 1: FINKI Moodle navigation sidebar — links with title attribute holding full name
        // e.g. <a title="Агентно-базирани системи-2025/2026/L" href=".../course/view.php?id=123">Ас-2025/2026/L-48</a>
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

                var courseId = ParseCourseIdFromUrl(a.GetAttributeValue("href", ""));

                courses.Add(new ImportedCourse
                {
                    Name = cleanName,
                    FullName = raw,
                    CourseId = courseId,
                    CourseUrl = a.GetAttributeValue("href", ""),
                    AcademicYear = academicYear,
                    SemesterType = semesterType,
                    Semester = semesterType.Equals("L", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
                    Credits = 0,
                });
            }
        }

        // Strategy 2: Moodle course cards (standard Moodle dashboard structure)
        if (!courses.Any())
        {
            var moodleSelectors = new[]
            {
                "//a[contains(@class,'coursename')]",
                "//*[@data-region='course-card']//a[contains(@class,'aalink') or contains(@class,'coursename')]",
                "//*[contains(@class,'course-info-container')]//a",
                "//*[contains(@class,'dashboard-card')]//a[contains(@class,'coursename') or @data-type='course']",
                "//*[contains(@class,'course-card-title')]//a",
                "//a[contains(@href,'/course/view.php')]",
            };

            foreach (var xpath in moodleSelectors)
            {
                var nodes = doc.DocumentNode.SelectNodes(xpath);
                if (nodes == null) continue;

                foreach (var node in nodes)
                {
                    var name = HtmlEntity.DeEntitize(
                        node.GetAttributeValue("title", node.InnerText).Trim());
                    name = Regex.Replace(name, @"-\d{4}/\d{4}(/\w+)?$", "").Trim();
                    if (name.Length < 4 || name.Length > 120) continue;
                    if (Regex.IsMatch(name, @"^\d|^(No events|Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)", RegexOptions.IgnoreCase)) continue;
                    if (courses.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) continue;
                    courses.Add(new ImportedCourse { Name = name, Credits = 0, Semester = 1 });
                }

                if (courses.Any()) break;
            }
        }

        // Strategy 2: any table with a header column matching "course/subject/предмет"
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

                if (nameIdx < 0) continue; // skip tables with no recognisable name column

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

        // Strategy 3: any anchor pointing to /course/view.php (broader Moodle fallback)
        if (!courses.Any())
        {
            var links = doc.DocumentNode.SelectNodes("//a[contains(@href,'course/view')]")
                        ?? Enumerable.Empty<HtmlNode>();
            foreach (var a in links)
            {
                var name = HtmlEntity.DeEntitize(a.InnerText.Trim());
                if (name.Length < 4 || name.Length > 120) continue;
                if (Regex.IsMatch(name, @"^\d|^(No events|Mon|Tue|Wed|Thu|Fri|Sat|Sun)", RegexOptions.IgnoreCase)) continue;
                if (courses.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) continue;
                courses.Add(new ImportedCourse { Name = name, Credits = 0, Semester = 1 });
            }
        }

        // Build a debug snippet of the raw HTML to help tune selectors
        var snippet = html.Length > 2000 ? html[..2000] : html;

        if (!courses.Any())
            return new FinkiImportResult
            {
                Success = false,
                Error = "Could not detect any courses on the page. Paste the URL you see after logging in — the page structure may need custom selectors.",
                DebugHtmlSnippet = snippet
            };

        return new FinkiImportResult { Success = true, Courses = courses, DebugHtmlSnippet = snippet };
    }

    // Splits "Агентно-базирани системи-2025/2026/L" into ("Агентно-базирани системи", "2025/2026", "L").
    // Returns the original string with empty year/semester when no suffix is present
    // (e.g. admin courses like "Консултации").
    private static (string cleanName, string academicYear, string semesterType) ParseFinkiCourseName(string raw)
    {
        var trimmed = raw.Trim();
        var match = Regex.Match(trimmed, @"^(?<name>.+?)\s*-\s*(?<year>\d{4}/\d{4})(?:/(?<sem>[A-Za-zЗзЛл]+))?\s*$");
        if (!match.Success)
            return (trimmed, "", "");

        var name = match.Groups["name"].Value.Trim();
        var year = match.Groups["year"].Value;
        var sem = match.Groups["sem"].Success ? match.Groups["sem"].Value.ToUpperInvariant() : "";
        return (name, year, sem);
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

    private static bool IsTrue(string s) =>
        Regex.IsMatch(s, @"yes|да|true|1|\+", RegexOptions.IgnoreCase);

    private static int ParseCreditsFromText(string text)
    {
        var m = Regex.Match(text, @"(\d+)\s*(ECTS|кредит)", RegexOptions.IgnoreCase);
        return m.Success ? ParseInt(m.Groups[1].Value) : 0;
    }

    private static string BuildAbsoluteUrl(string baseUrl, string action)
    {
        if (string.IsNullOrEmpty(action)) return baseUrl;
        if (action.StartsWith("http")) return action;
        var uri = new Uri(baseUrl);
        return action.StartsWith("/")
            ? $"{uri.Scheme}://{uri.Host}{action}"
            : $"{uri.Scheme}://{uri.Host}/{action}";
    }

    private static FinkiImportResult Fail(string msg) =>
        new() { Success = false, Error = msg };
}