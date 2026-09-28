using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace Flowzy.Service.Feedback;

public sealed class AdminFeedbackService(IFeedbackRepository feedbacks) : IAdminFeedbackService
{
    private static readonly TimeZoneInfo Ict = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public async Task<PageResponse<AdminFeedbackResponse>> SearchAsync(int page, int size, string? term,
        string? courseCode, FeedbackTargetType? targetType, long? targetId, string? targetSearch,
        FeedbackStatus? status, CancellationToken cancellationToken = default)
    {
        if (page < 0) throw new BadRequestException("Page index must be zero or greater");
        if (size is < 1 or > 100) throw new BadRequestException("Page size must be between 1 and 100");
        var result = await feedbacks.FindAdminAsync(page, size, term, courseCode, targetType?.ToString(), targetId,
            targetSearch, status?.ToString(), cancellationToken);
        return PageResponse<AdminFeedbackResponse>.Create(result.Items.Select(Map).ToList(), page, size, result.Total);
    }

    public async Task<byte[]> ExportAsync(string? term, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term)) throw new BadRequestException("Academic term is required");
        var normalized = term.Trim().ToUpperInvariant();
        var academicTerm = await feedbacks.FindTermAsync(normalized, cancellationToken)
            ?? throw new NotFoundException($"Academic term not found: {normalized}");
        var items = await feedbacks.FindSubmittedForExportAsync(academicTerm.Code, cancellationToken);
        items = items.OrderBy(TargetCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Group.CourseCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Group.GroupNo, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Student.StudentCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.SubmittedAt ?? DateTime.MaxValue).ThenBy(x => x.Id).ToList();
        using var workbook = new XSSFWorkbook();
        WriteSheet(workbook, "Mentor Feedback", "MENTOR", items);
        WriteSheet(workbook, "Instructor Feedback", "INSTRUCTOR", items);
        using var stream = new MemoryStream();
        workbook.Write(stream, true);
        return stream.ToArray();
    }

    private static void WriteSheet(IWorkbook workbook, string sheetName, string targetType,
        IEnumerable<TermFeedback> items)
    {
        var label = targetType == "MENTOR" ? "Mentor" : "Instructor";
        string[] headers = ["Feedback ID", "Term", "Course", "Group No", "Group Name", "Project",
            "Student Code", "Student Name", "Student Email", $"{label} Code", $"{label} Name",
            $"{label} Email", "Rating", "Comment", "Submitted At (ICT)"];
        var sheet = workbook.CreateSheet(sheetName);
        var headerFont = workbook.CreateFont(); headerFont.IsBold = true; headerFont.Color = IndexedColors.White.Index;
        var headerStyle = workbook.CreateCellStyle(); headerStyle.SetFont(headerFont);
        headerStyle.FillForegroundColor = IndexedColors.Orange.Index; headerStyle.FillPattern = FillPattern.SolidForeground;
        headerStyle.Alignment = HorizontalAlignment.Center; headerStyle.WrapText = true;
        var body = workbook.CreateCellStyle(); body.VerticalAlignment = VerticalAlignment.Top;
        var wrapped = workbook.CreateCellStyle(); wrapped.CloneStyleFrom(body); wrapped.WrapText = true;
        var date = workbook.CreateCellStyle(); date.CloneStyleFrom(body);
        date.DataFormat = workbook.CreateDataFormat().GetFormat("dd/mm/yyyy hh:mm");
        var headerRow = sheet.CreateRow(0); headerRow.HeightInPoints = 32;
        for (var i = 0; i < headers.Length; i++) { var c = headerRow.CreateCell(i); c.SetCellValue(headers[i]); c.CellStyle = headerStyle; }
        sheet.CreateFreezePane(0, 1);
        var rowIndex = 1;
        foreach (var item in items.Where(x => x.TargetType == targetType))
        {
            var row = sheet.CreateRow(rowIndex++);
            SetNumber(row, 0, item.Id, body); SetText(row, 1, item.AcademicTerm.Code, body);
            SetText(row, 2, item.Group.CourseCode, body); SetText(row, 3, item.Group.GroupNo, body);
            SetText(row, 4, item.Group.Name, wrapped); SetText(row, 5, item.Group.ProjectName, wrapped);
            SetText(row, 6, item.Student.StudentCode, body); SetText(row, 7, item.Student.FullName, wrapped);
            SetText(row, 8, item.Student.Account.Email, body); SetText(row, 9, TargetCode(item), body);
            SetText(row, 10, TargetName(item), wrapped); SetText(row, 11, TargetEmail(item), body);
            if (item.Rating.HasValue) SetNumber(row, 12, item.Rating.Value, body); else row.CreateCell(12).CellStyle = body;
            SetText(row, 13, item.Comment, wrapped);
            var dateCell = row.CreateCell(14);
            if (item.SubmittedAt.HasValue) dateCell.SetCellValue(TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(item.SubmittedAt.Value, DateTimeKind.Utc), Ict));
            dateCell.CellStyle = date;
        }
        sheet.SetAutoFilter(new CellRangeAddress(0, Math.Max(rowIndex - 1, 0), 0, headers.Length - 1));
        for (var i = 0; i < headers.Length; i++) { sheet.AutoSizeColumn(i); sheet.SetColumnWidth(i, Math.Min(sheet.GetColumnWidth(i) + 512, 60 * 256)); }
    }

    private static void SetText(IRow row, int index, string? value, ICellStyle style)
    { var cell = row.CreateCell(index); cell.SetCellValue(value ?? string.Empty); cell.CellStyle = style; }
    private static void SetNumber(IRow row, int index, long value, ICellStyle style)
    { var cell = row.CreateCell(index); cell.SetCellValue(value); cell.CellStyle = style; }
    private static string TargetCode(TermFeedback x) => x.TargetType == "MENTOR" ? x.Mentor!.MentorCode : x.Instructor!.InstructorCode;
    private static string TargetName(TermFeedback x) => x.TargetType == "MENTOR" ? x.Mentor!.FullName : x.Instructor!.FullName;
    private static string? TargetEmail(TermFeedback x) => x.TargetType == "MENTOR" ? x.Mentor?.Account.Email : x.Instructor?.Account.Email;

    private static AdminFeedbackResponse Map(TermFeedback x) => new(x.Id,
        new FeedbackTermResponse(x.AcademicTerm.Id, x.AcademicTerm.Code, x.AcademicTerm.Status),
        new FeedbackGroupResponse(x.Group.Id, x.Group.Term, x.Group.CourseCode, x.Group.GroupNo, x.Group.Name, x.Group.ProjectName),
        new FeedbackStudentResponse(x.Student.Id, x.Student.StudentCode, x.Student.FullName, x.Student.Account?.Email),
        x.TargetType,
        x.Mentor is null ? null : new FeedbackMentorResponse(x.Mentor.Id, x.Mentor.MentorCode, x.Mentor.FullName, x.Mentor.Account?.Email),
        x.Instructor is null ? null : new FeedbackInstructorResponse(x.Instructor.Id, x.Instructor.InstructorCode, x.Instructor.FullName, x.Instructor.Account?.Email),
        x.Rating, x.Comment, x.Status, x.SubmittedAt, x.CreatedAt, x.UpdatedAt);
}
