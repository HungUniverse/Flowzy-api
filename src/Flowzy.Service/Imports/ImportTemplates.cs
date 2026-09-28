using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace Flowzy.Service.Imports;

public static class ImportTemplates
{
    public static byte[] Create(string target)
    {
        using var book = new XSSFWorkbook();
        if (target == "STUDENT_ACCOUNT")
        {
            var sheet = Table(book, "Student Accounts", 0, ["RollNumber", "Fullname", "Email", "SubjectCode", "GroupName"], [18, 28, 32, 18, 18]);
            var helper = sheet.GetDataValidationHelper(); var validation = helper.CreateValidation(helper.CreateExplicitListConstraint(["EXE101", "EXE201"]), new CellRangeAddressList(1, 40, 3, 3));
            validation.CreateErrorBox("Invalid SubjectCode", "Choose EXE101 or EXE201."); validation.ShowErrorBox = true; sheet.AddValidationData(validation);
        }
        else if (target == "STUDENT") Table(book, "SU26_EXE101 | Group List", 0,
            ["STT", "Tên Nhóm", "Tên dự án\n(có thể trùng tên nhóm)", "Mô tả ý tưởng chi tiết\n(SV điền sau Checkpoint 1)", "Phân bổ về LAB", "Domain Nghiên Cứu của Ý Tưởng\n(có thể chọn nhiều hơn 1)", "Họ & Tên", "MSSV", "Ngành", "Chuyên Ngành", "Email (email trên FAP của SV)", "Mã lớp & GV FAP EXE101", "Họ & Tên Leader", "Số điện thoại có Zalo của Leader", "GV chấm điểm Checkpoint team muốn tại EXE101", "Điều tiết GV của bộ môn (Final)", "Note", "Mentor phân bổ (đề xuất)"],
            [5, 12, 22, 48, 18, 26, 24, 12, 18, 18, 32, 26, 24, 18, 32, 28, 18, 40]);
        else if (target == "MENTOR")
        {
            var sheet = Table(book, "Sheet1", 4,
                ["Mentor ID", "Mentor Name", "Academic Degree", "Email", "Mobile", "Company", "Title/Position", "Experience years", "Affiliation", "Domain Expertise", "Domain Code (D01-D18)", "Track A/B/C", "Mức độ ưu tiên 1", "Mức độ ưu tiên 2", "Mentor Type", "Primary Capability Supported", "Best EXE Stage", "Can Support Branches", "Suggested Role", "Availability", "Assigned Projects", "Status", "Notes"],
                [8, 24, 14, 26, 14, 26, 24, 16, 24, 38, 22, 18, 16, 16, 24, 30, 20, 24, 24, 18, 42, 18, 28]);
            sheet.CreateRow(0).CreateCell(0).SetCellValue("Mentor Matrix");
            sheet.CreateRow(1).CreateCell(0).SetCellValue("Gán mentor theo năng lực và domain; ưu tiên persistent mentorship từ CP1 đến KLTN khi phù hợp");
        }
        else if (target == "PROBLEM_BANK")
        {
            var sheet = Table(book, "domain nghien cuu", 2,
                ["Domain Code", "Macro Domain", "Sub-domain / Problem Field", "Typical Problem Examples", "Primary Discipline", "Supporting Disciplines", "Best Problem Sources", "Relevant Student Capabilities", "Potential EXE Outputs", "Notes"], [14, 24, 34, 42, 24, 30, 26, 34, 28, 32]);
            sheet.CreateRow(0).CreateCell(0).SetCellValue("DOMAIN NGHIÊN CỨU");
            Table(book, "Research Questions", 0, ["", "Strategic_Theme", "Research_Area", "Research_Topic", "Research_Question", "Difficulty_Level", "Owner_Lab", "Suggested_Courses", "Expected_Output", "Status", "Drive_Folder_Link"], [13, 28, 34, 54, 80, 16, 18, 60, 22, 14, 34]);
        }
        else throw new ArgumentException("Unsupported import target", nameof(target));
        using var output = new MemoryStream(); book.Write(output, true); return output.ToArray();
    }
    private static ISheet Table(IWorkbook book, string name, int headerIndex, string[] headers, int[] widths)
    {
        var sheet = book.CreateSheet(name); var style = book.CreateCellStyle(); var font = book.CreateFont(); font.IsBold = true;
        style.SetFont(font); style.WrapText = true; style.VerticalAlignment = VerticalAlignment.Center;
        style.BorderBottom = style.BorderTop = style.BorderLeft = style.BorderRight = BorderStyle.Thin;
        var header = sheet.CreateRow(headerIndex); header.HeightInPoints = 40;
        for (var c = 0; c < headers.Length; c++) { var cell = header.CreateCell(c); cell.SetCellValue(headers[c]); cell.CellStyle = style; sheet.SetColumnWidth(c, widths[c] * 256); }
        for (var r = headerIndex + 1; r <= headerIndex + 40; r++) { var row = sheet.CreateRow(r); row.HeightInPoints = 28; for (var c = 0; c < headers.Length; c++) row.CreateCell(c); }
        sheet.CreateFreezePane(0, headerIndex + 1); return sheet;
    }
}
