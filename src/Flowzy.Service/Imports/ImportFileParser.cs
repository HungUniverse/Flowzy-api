using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Flowzy.Service.Exceptions;
using NPOI.SS.UserModel;

namespace Flowzy.Service.Imports;

public sealed record ImportRow(int Number, Dictionary<string, string> Data, string? Sheet = null, string? Type = null)
{
    public string Value(params string[] keys)
    {
        foreach (var key in keys) if (Data.TryGetValue(key, out var value)) return value.Trim();
        return "";
    }
}

public static class ImportFileParser
{
    private static readonly string[] GroupFields = ["group_no", "group_name", "project_name", "idea_description", "research_domain", "leader_full_name", "mentor_assignment"];
    private static readonly Dictionary<string, string> RosterHeaders = new()
    { ["rollnumber"] = "roll_number", ["fullname"] = "full_name", ["email"] = "email", ["subjectcode"] = "subject_code", ["groupname"] = "group_name" };
    private static readonly HashSet<string> AccountHeaders = new("student_code mentor_code full_name email password phone date_of_birth gender address major cohort class_name job_title company expertise years_of_experience linkedin_url status role group_no group_name project_name idea_description research_domain leader_full_name mentor_assignment".Split(' '));

    public static IReadOnlyList<ImportRow> Parse(Stream input, string fileType, string target)
    {
        if (fileType == "CSV")
        {
            using var reader = new StreamReader(input, Encoding.UTF8, true, leaveOpen: true);
            using var csv = new CsvParser(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            { HasHeaderRecord = false, TrimOptions = TrimOptions.Trim, IgnoreBlankLines = true });
            var records = new List<string[]>();
            while (csv.Read()) records.Add(csv.Record!.ToArray());
            if (records.Count == 0) throw new BadRequestException(target == "STUDENT_ACCOUNT" || target == "PROBLEM_BANK" ? "File contains no CSV data" : "File is empty");
            return ParseTable(records, null, target, false, null);
        }
        using var workbook = WorkbookFactory.Create(input);
        if (workbook.NumberOfSheets == 0) throw new BadRequestException("No sheets found in workbook");
        var formatter = new DataFormatter(CultureInfo.InvariantCulture);
        var sheets = Enumerable.Range(0, workbook.NumberOfSheets).Select(workbook.GetSheetAt).ToList();
        var result = new List<ImportRow>();
        if (target == "STUDENT_ACCOUNT")
        {
            var supported = false;
            foreach (var sheet in sheets)
            {
                var rows = ReadSheet(sheet, formatter);
                var first = rows.FirstOrDefault(x => x.Any(v => !string.IsNullOrWhiteSpace(v)));
                if (first is null || !first.Any(x => RosterHeaders.ContainsKey(RosterHeader(x)))) continue;
                supported = true; result.AddRange(ParseTable(rows, sheet.SheetName, target, true, null));
            }
            if (!supported) throw new BadRequestException("No supported student-account import header found");
        }
        else if (target == "PROBLEM_BANK")
        {
            var domain = sheets.LastOrDefault(x => Normalize(x.SheetName).Contains("domain", StringComparison.Ordinal));
            var problem = sheets.LastOrDefault(x => !Normalize(x.SheetName).Contains("domain", StringComparison.Ordinal) && (Normalize(x.SheetName).Contains("problem", StringComparison.Ordinal) || Normalize(x.SheetName).Contains("questions", StringComparison.Ordinal)));
            if (domain is not null && problem is not null)
            {
                result.AddRange(ParseTable(ReadSheet(domain, formatter), domain.SheetName, target, true, "domain"));
                result.AddRange(ParseTable(ReadSheet(problem, formatter), problem.SheetName, target, true, "problem"));
            }
            else result.AddRange(ParseTable(ReadSheet(sheets[0], formatter), sheets[0].SheetName, target, true, null));
        }
        else
        {
            var main = sheets.LastOrDefault(x => Normalize(x.SheetName).Contains("group_list", StringComparison.Ordinal));
            var free = sheets.LastOrDefault(x => Normalize(x.SheetName).Contains("sv_tim_nhom", StringComparison.Ordinal));
            main ??= sheets[0] != free ? sheets[0] : null;
            if (main is not null) result.AddRange(ParseTable(ReadSheet(main, formatter), main.SheetName, target, true, "main"));
            if (free is not null) result.AddRange(ParseTable(ReadSheet(free, formatter), free.SheetName, target, true, "free"));
        }
        return result;
    }

    private static List<string[]> ReadSheet(ISheet sheet, DataFormatter formatter) => Enumerable.Range(0, sheet.LastRowNum + 1)
        .Select(index => sheet.GetRow(index) is { } row
            ? Enumerable.Range(0, Math.Max((int)row.LastCellNum, 0)).Select(c => formatter.FormatCellValue(row.GetCell(c))).ToArray() : Array.Empty<string>()).ToList();

    private static IReadOnlyList<ImportRow> ParseTable(List<string[]> records, string? sheet, string target, bool xlsx, string? type)
    {
        var headerIndex = -1; string[] headers = [];
        var scan = target == "STUDENT_ACCOUNT" ? records.Select((r, i) => i).Where(i => records[i].Any(v => !string.IsNullOrWhiteSpace(v))).Take(1)
            : Enumerable.Range(0, xlsx ? records.Count : Math.Min(records.Count, 20));
        if (xlsx && type == "main") scan = new[] { 0, 4 }.Where(i => i < records.Count).Concat(scan.Where(i => i != 0 && i != 4));
        foreach (var i in scan)
        {
            if (target == "STUDENT_ACCOUNT") { headers = RosterLayout(records[i]); headerIndex = i; break; }
            var mapped = records[i].Select(value => target == "PROBLEM_BANK" ? ProblemHeader(value) : AccountHeader(value)).ToArray();
            if (target != "PROBLEM_BANK" && mapped.Contains("role")) throw new BadRequestException("UNSUPPORTED_COLUMN");
            var supported = target == "PROBLEM_BANK" ? mapped.Contains("code") || mapped.Contains("type") || (xlsx && mapped.Contains("title"))
                : mapped.Contains("email") && mapped.Contains("full_name") && (mapped.Contains("student_code") || mapped.Contains("mentor_code"));
            if (supported) { headers = mapped; headerIndex = i; break; }
        }
        if (headerIndex < 0)
        {
            if (target == "PROBLEM_BANK" && xlsx) return [];
            throw new BadRequestException(target == "PROBLEM_BANK" ? "No supported header row found in CSV file" : "No supported import header found");
        }
        var rows = new List<ImportRow>(); var context = new Dictionary<string, string>();
        for (var i = headerIndex + 1; i < records.Count; i++)
        {
            var record = records[i]; if (record.All(string.IsNullOrWhiteSpace)) continue;
            var data = new Dictionary<string, string>();
            for (var c = 0; c < headers.Length; c++)
            {
                var key = headers[c]; if (key.Length == 0 && type == "problem" && c == 0) key = "code";
                if (key.Length != 0) data[key] = c < record.Length ? record[c].Trim() : "";
            }
            var rowType = type;
            if (target == "PROBLEM_BANK")
            {
                rowType ??= data.GetValueOrDefault("type", "").Trim().ToLowerInvariant();
                if (rowType.Length == 0) rowType = data.GetValueOrDefault("title", "").Length != 0 || data.GetValueOrDefault("statement", "").Length != 0 ? "problem" : "domain";
                rowType = rowType == "problem" ? "problem" : "domain";
                if (rowType == "domain") { Copy(data, "code", "domain_code"); Copy(data, "name", "macro_domain"); Copy(data, "description", "sub_domain"); }
                else Copy(data, "domain_code", "domain");
            }
            else if (type == "main")
            {
                if (!string.IsNullOrWhiteSpace(data.GetValueOrDefault("group_no"))) { context.Clear(); foreach (var key in GroupFields) context[key] = data.GetValueOrDefault(key, ""); }
                else foreach (var pair in context) data[pair.Key] = pair.Value;
            }
            else if (type == "free") foreach (var key in GroupFields) data.Remove(key);
            rows.Add(new(i + 1, data, sheet, rowType));
        }
        return rows;
    }

    private static void Copy(Dictionary<string, string> data, string target, string source)
    { if (string.IsNullOrWhiteSpace(data.GetValueOrDefault(target)) && !string.IsNullOrWhiteSpace(data.GetValueOrDefault(source))) data[target] = data[source]; }
    private static string RosterHeader(string value) => value.Replace("\uFEFF", "", StringComparison.Ordinal).Trim().ToLowerInvariant();
    private static string[] RosterLayout(string[] raw)
    {
        if (raw.Length != 5) throw new BadRequestException("UNSUPPORTED_COLUMN: The header row must contain exactly five columns");
        var found = new HashSet<string>(); var headers = new List<string>();
        foreach (var value in raw)
        {
            var normalized = RosterHeader(value);
            if (normalized.Length == 0) { headers.Add(""); continue; }
            if (!RosterHeaders.TryGetValue(normalized, out var canonical)) throw new BadRequestException("UNSUPPORTED_COLUMN: Unsupported column: " + value.Trim());
            if (!found.Add(canonical)) throw new BadRequestException("UNSUPPORTED_COLUMN: Duplicate column: " + value.Trim());
            headers.Add(canonical);
        }
        if (found.Count != 5) throw new BadRequestException("UNSUPPORTED_COLUMN: Missing required columns: " + string.Join(", ", RosterHeaders.Values.Except(found)));
        return headers.ToArray();
    }

    public static string Normalize(string value)
    {
        value = value.Trim().Replace('Đ', 'D').Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        value = Regex.Replace(value, "[\\u0300-\\u036f]+", "").ToLowerInvariant();
        return Regex.Replace(value, "[^a-z0-9]+", "_").Trim('_');
    }
    private static string AccountHeader(string value)
    {
        var n = Normalize(value);
        if (n.StartsWith("mentor_phan_bo", StringComparison.Ordinal) || n.StartsWith("mentor_exe", StringComparison.Ordinal)) return "mentor_assignment";
        if (n.StartsWith("ten_du_an", StringComparison.Ordinal)) return "project_name";
        if (n.StartsWith("mo_ta_y_tuong", StringComparison.Ordinal)) return "idea_description";
        if (n.StartsWith("domain_nghien_cuu", StringComparison.Ordinal)) return "research_domain";
        if (n.StartsWith("ho_ten_leader", StringComparison.Ordinal)) return "leader_full_name";
        n = n switch
        {
            "ho_ten" or "ho_ten_sinh_vien_tim_nhom_exe101sp26" or "mentor_name" => "full_name",
            "mssv" => "student_code", "nganh" or "ten_nganh" or "chuyen_nganh" => "major",
            "email_email_tren_fap_cua_sv" or "email_duoi_fpt" or "email_uoi_fpt" => "email",
            "sdt_optional" or "mobile" => "phone", "ma_lop_gv_fap_exe101" or "ma_lop_gv_fap_exe201" => "class_name",
            "mentor_id" => "mentor_code", "title_position" => "job_title", "domain_expertise" => "expertise",
            "experience_years" => "years_of_experience", "linkedin" => "linkedin_url", "stt" => "group_no", "ten_nhom" => "group_name", _ => n
        };
        return AccountHeaders.Contains(n) ? n : "";
    }
    private static string ProblemHeader(string value) => Normalize(value) switch
    {
        "ma_linh_vuc" or "domain_code" or "ma_domain" => "domain_code",
        "ma_de_tai" or "problem_code" or "code" => "code", "row_type" or "loai" => "type",
        "ten_domain" or "domain_name" or "ten_linh_vuc" or "macro_domain" => "name",
        "mo_ta" or "mo_ta_linh_vuc" or "sub_domain_problem_field" => "description",
        "typical_problem_examples" => "typical_examples", "best_problem_sources" => "best_sources",
        "relevant_student_capabilities" => "student_capabilities", "potential_exe_outputs" => "potential_outputs", "ghi_chu" => "notes",
        "ten_de_tai" or "problem_title" or "research_topic" => "title",
        "mo_ta_de_tai" or "problem_statement" or "cau_hoi_nghien_cuu" or "research_question" => "statement",
        "muc_do_kho" or "difficulty" => "difficulty_level", "chu_de_chien_luoc" or "theme" => "strategic_theme",
        "linh_vuc_nghien_cuu" or "area" => "research_area", "ket_qua_dau_ra" => "expected_output", "trang_thai" => "status", _ => Normalize(value)
    };
}
