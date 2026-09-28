using System.Globalization;
using System.Text.RegularExpressions;

namespace Flowzy.Service.Imports;

public sealed record ImportIssue(string? Field, string Code, string Message);

public static class ImportValidation
{
    public static IReadOnlyList<ImportIssue> AccountRow(ImportRow row, string target, ISet<string> emails,
        ISet<string> codes, ISet<string> seenEmails, ISet<string> seenCodes, bool reactivate)
    {
        var roster = target == "STUDENT_ACCOUNT"; var mentor = target == "MENTOR";
        var codeField = roster ? "roll_number" : mentor ? "mentor_code" : "student_code";
        var email = roster ? row.Value("email") : row.Value("email", "email_address");
        var code = roster ? row.Value(codeField) : row.Value(codeField, mentor ? "mentorcode" : "studentcode", "code");
        var name = roster ? row.Value("full_name") : row.Value("full_name", "fullname", "name");
        var issues = new List<ImportIssue>();
        if (!roster) Required(issues, "email", email, "Email is required");
        Required(issues, codeField, code, roster ? "RollNumber is required" : "Code is required");
        Required(issues, "full_name", name, roster ? "Fullname is required" : "Full name is required");
        if (roster) Required(issues, "email", email, "Email is required");
        if (roster)
        {
            Required(issues, "subject_code", row.Value("subject_code"), "SubjectCode is required");
            Required(issues, "group_name", row.Value("group_name"), "GroupName is required");
            Maximum(issues, codeField, code, 50, "RollNumber must not exceed 50 characters");
            Maximum(issues, "full_name", name, 255, "Fullname must not exceed 255 characters");
            Maximum(issues, "email", email, 255, "Email must not exceed 255 characters");
            Maximum(issues, "group_name", row.Value("group_name"), 100, "GroupName must not exceed 100 characters");
        }
        var emailPattern = "^[a-zA-Z0-9_+&*-]+(?:\\.[a-zA-Z0-9_+&*-]+)*@(?:[a-zA-Z0-9-]+\\.)+[a-zA-Z]{2," + (roster ? "63" : "7") + "}$";
        if (email.Length != 0 && !Regex.IsMatch(email, emailPattern)) issues.Add(new("email", "INVALID_EMAIL", "Invalid email format"));
        if (roster && row.Value("subject_code").Length != 0 && row.Value("subject_code").ToUpperInvariant() is not ("EXE101" or "EXE201")) issues.Add(new("subject_code", "INVALID_VALUE", "SubjectCode must be EXE101 or EXE201"));
        if (email.Length != 0)
        {
            if (!seenEmails.Add(email.ToLowerInvariant())) issues.Add(new("email", "DUPLICATED_IN_FILE", "Duplicate email in import file"));
            if (roster && emails.Contains(email.ToLowerInvariant()) && !reactivate) issues.Add(new("email", "ALREADY_EXISTS", "Email already exists in database"));
        }
        if (code.Length != 0)
        {
            if (!seenCodes.Add(code.ToLowerInvariant())) issues.Add(new(codeField, "DUPLICATED_IN_FILE", roster ? "RollNumber is duplicated in import file" : "Duplicate code in import file"));
            if (roster && codes.Contains(code.ToLowerInvariant()) && !reactivate) issues.Add(new(codeField, "ALREADY_EXISTS", "RollNumber already exists in database"));
        }
        if (!roster)
        {
            if (email.Length != 0 && emails.Contains(email.ToLowerInvariant()) && !reactivate) issues.Add(new("email", "ALREADY_EXISTS", "Email already exists in database"));
            if (code.Length != 0 && codes.Contains(code.ToLowerInvariant()) && !reactivate) issues.Add(new(codeField, "ALREADY_EXISTS", "Code already exists in database"));
            if (!mentor)
            {
                var gender = row.Value("gender", "sex"); if (gender.Length != 0 && gender.ToUpperInvariant() is not ("MALE" or "FEMALE" or "OTHER")) issues.Add(new("gender", "INVALID_GENDER", "Invalid gender value"));
                var dob = row.Value("date_of_birth", "dateofbirth", "dob", "birthday"); if (dob.Length != 0 && Date(dob) is null) issues.Add(new("date_of_birth", "INVALID_DATE", "Invalid date of birth format"));
            }
            else try { Experience(row.Value("years_of_experience", "yearsofexperience", "experience")); } catch (ArgumentException e) { issues.Add(new("years_of_experience", "INVALID_EXPERIENCE", e.Message)); }
            var password = row.Value("password"); if (password.Length is > 0 and < 8) issues.Add(new("password", "INVALID_FORMAT", "Password must be at least 8 characters"));
        }
        return issues;
    }
    public static IReadOnlyList<ImportIssue> ProblemRow(ImportRow row, ISet<string> seen)
    {
        var domain = row.Type == "domain"; var kind = domain ? "Domain" : "Problem"; var issues = new List<ImportIssue>();
        var code = row.Value("code"); Required(issues, "code", code, kind + " code is required");
        if (code.Length != 0 && !seen.Add(code.ToLowerInvariant())) issues.Add(new("code", "DUPLICATED_IN_FILE", "Duplicate " + kind.ToLowerInvariant() + " code in import file"));
        Maximum(issues, "code", code, domain ? 50 : 100, kind + " code must be " + (domain ? "50" : "100") + " characters or less");
        var field = domain ? "name" : "title"; var label = domain ? "Domain name" : "Problem title";
        Required(issues, field, row.Value(field), label + " is required"); Maximum(issues, field, row.Value(field), 255, label + " must be 255 characters or less");
        if (!domain)
        {
            Required(issues, "statement", row.Value("statement"), "Problem statement is required");
            var difficulty = row.Value("difficulty_level"); Required(issues, "difficulty_level", difficulty, "Difficulty level is required");
            if (difficulty.Length != 0 && Difficulty(difficulty) is null) issues.Add(new("difficulty_level", "INVALID_VALUE", "Difficulty level must be BEGINNER, INTERMEDIATE, or ADVANCED"));
        }
        var status = row.Value("status").ToUpperInvariant();
        if (status.Length != 0 && status is not ("ACTIVE" or "INACTIVE")) issues.Add(new("status", "INVALID_VALUE", kind + " status must be either ACTIVE or INACTIVE"));
        return issues;
    }
    public static string? Difficulty(string text)
    {
        var n = text.Trim().ToLowerInvariant();
        if (n.Contains("beginer", StringComparison.Ordinal) || n.Contains("beginner", StringComparison.Ordinal) || n == "easy") return "BEGINNER";
        if (n.Contains("intermediate", StringComparison.Ordinal) || n == "medium") return "INTERMEDIATE";
        return n.Contains("advanced", StringComparison.Ordinal) || n == "hard" ? "ADVANCED" : null;
    }
    public static DateOnly? Date(string value)
    {
        // Java DateTimeFormatter.ofPattern uses SMART resolution: day 29-31 is
        // clamped to the end of a shorter month, but day 0 or 32 is invalid.
        foreach (var format in new[] { @"^(?<y>\d{4})-(?<m>\d{2})-(?<d>\d{2})$", @"^(?<m>\d{2})/(?<d>\d{2})/(?<y>\d{4})$",
                     @"^(?<d>\d{2})/(?<m>\d{2})/(?<y>\d{4})$", @"^(?<y>\d{4})/(?<m>\d{2})/(?<d>\d{2})$" })
        {
            var match = Regex.Match(value.Trim(), format); if (!match.Success) continue;
            var year = int.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture);
            var month = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
            var day = int.Parse(match.Groups["d"].Value, CultureInfo.InvariantCulture);
            if (year is < 1 or > 9999 || month is < 1 or > 12 || day is < 1 or > 31) continue;
            return new DateOnly(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
        }
        return null;
    }
    public static int? Experience(string value)
    {
        var n = ImportFileParser.Normalize(value);
        if (n.Length == 0) return null;
        if (n == "tren_10_nam") return 10; if (n == "tu_5_10_nam") return 5; if (n == "tu_2_5_nam") return 2;
        if (!int.TryParse(value.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number)) throw new ArgumentException("INVALID_EXPERIENCE");
        if (number < 0) throw new ArgumentException("Negative experience: " + number.ToString(CultureInfo.InvariantCulture)); return number;
    }
    private static void Required(List<ImportIssue> issues, string field, string value, string message) { if (value.Length == 0) issues.Add(new(field, "MISSING_REQUIRED_FIELD", message)); }
    private static void Maximum(List<ImportIssue> issues, string field, string value, int max, string message) { if (value.Length > max) issues.Add(new(field, "INVALID_FORMAT", message)); }
}
