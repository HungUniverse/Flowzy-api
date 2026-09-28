using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace Flowzy.Service.Mentors;

public sealed class MentorMeetingReportService(IAccountRepository accounts,
    IMentorMeetingReportRepository reports) : IMentorMeetingReportService
{
    private static readonly string[] SummaryHeaders =
    [
        "Term", "Course", "Group No", "Group Name", "Project", "Total Meetings", "Scheduled",
        "Completed", "Canceled"
    ];
    private static readonly string[] DetailHeaders =
    [
        "Term", "Course", "Group No", "Group Name", "Project", "Start (ICT)", "End (ICT)", "Status",
        "Note", "Meeting Link", "Leader Confirmed At (ICT)", "Mentor Confirmed At (ICT)", "Evidence URL",
        "Evidence Submitted By", "Evidence Submitted At (ICT)", "Completed At (ICT)", "Canceled At (ICT)",
        "Cancel Reason"
    ];
    private static readonly TimeZoneInfo BusinessZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public async Task<IReadOnlyList<MentorReportTermResponse>> ListTermsAsync(string mentorEmail,
        CancellationToken cancellationToken = default)
    {
        var mentor = await RequireMentorAsync(mentorEmail, cancellationToken);
        return (await reports.FindTermsAsync(mentor.Id, cancellationToken))
            .Select(x => new MentorReportTermResponse(x.Code, x.Status)).ToList();
    }

    public async Task<byte[]> ExportAsync(string? term, string mentorEmail,
        CancellationToken cancellationToken = default)
    {
        var mentor = await RequireMentorAsync(mentorEmail, cancellationToken);
        if (string.IsNullOrWhiteSpace(term)) throw new BadRequestException("Academic term is required");
        var normalized = term.Trim().ToUpperInvariant();
        var academicTerm = await reports.FindTermAsync(normalized, cancellationToken)
            ?? throw new NotFoundException($"Academic term not found: {normalized}");
        var groups = await reports.FindGroupsAsync(mentor.Id, academicTerm.Code, cancellationToken);
        var meetings = await reports.FindMeetingsAsync(mentor.Id, academicTerm.Code, cancellationToken);

        using var workbook = new XSSFWorkbook();
        var styles = CreateStyles(workbook);
        WriteSummary(workbook, styles, groups, meetings);
        WriteDetails(workbook, styles, meetings);
        using var output = new MemoryStream();
        workbook.Write(output, true);
        return output.ToArray();
    }

    private async Task<Mentor> RequireMentorAsync(string email, CancellationToken cancellationToken)
    {
        var account = await accounts.FindByEmailAsync(email, cancellationToken);
        return account?.Mentor ?? throw new ForbiddenException($"Mentor profile not found for user: {email}");
    }

    private static void WriteSummary(IWorkbook workbook, Styles styles, IReadOnlyList<StudentGroup> groups,
        IReadOnlyList<MentorMeeting> meetings)
    {
        var sheet = workbook.CreateSheet("Group Summary");
        WriteHeader(sheet, styles, SummaryHeaders);
        var counts = meetings.GroupBy(x => x.GroupId).ToDictionary(x => x.Key,
            x => x.GroupBy(m => m.Status).ToDictionary(status => status.Key, status => status.LongCount()));
        var rowIndex = 1;
        foreach (var group in groups)
        {
            counts.TryGetValue(group.Id, out var statuses);
            var scheduled = statuses?.GetValueOrDefault("SCHEDULED") ?? 0;
            var completed = statuses?.GetValueOrDefault("COMPLETED") ?? 0;
            var canceled = statuses?.GetValueOrDefault("CANCELED") ?? 0;
            var row = sheet.CreateRow(rowIndex++);
            WriteText(row, 0, group.Term, styles.Body);
            WriteText(row, 1, group.CourseCode, styles.Body);
            WriteText(row, 2, group.GroupNo, styles.Body);
            WriteText(row, 3, group.Name, styles.Wrapped);
            WriteText(row, 4, group.ProjectName, styles.Wrapped);
            WriteNumber(row, 5, scheduled + completed + canceled, styles.Body);
            WriteNumber(row, 6, scheduled, styles.Body);
            WriteNumber(row, 7, completed, styles.Body);
            WriteNumber(row, 8, canceled, styles.Body);
        }
        FinishSheet(sheet, rowIndex - 1, SummaryHeaders.Length - 1);
    }

    private static void WriteDetails(IWorkbook workbook, Styles styles, IReadOnlyList<MentorMeeting> meetings)
    {
        var sheet = workbook.CreateSheet("Meeting Details");
        WriteHeader(sheet, styles, DetailHeaders);
        var rowIndex = 1;
        foreach (var meeting in meetings)
        {
            var group = meeting.Group;
            var row = sheet.CreateRow(rowIndex++);
            WriteText(row, 0, group.Term, styles.Body);
            WriteText(row, 1, group.CourseCode, styles.Body);
            WriteText(row, 2, group.GroupNo, styles.Body);
            WriteText(row, 3, group.Name, styles.Wrapped);
            WriteText(row, 4, group.ProjectName, styles.Wrapped);
            WriteInstant(row, 5, meeting.StartAt, styles);
            WriteInstant(row, 6, meeting.EndAt, styles);
            WriteText(row, 7, meeting.Status, styles.Body);
            WriteText(row, 8, meeting.Note, styles.Wrapped);
            WriteLink(row, 9, meeting.MeetLink, workbook, styles);
            WriteInstant(row, 10, meeting.LeaderConfirmedAt, styles);
            WriteInstant(row, 11, meeting.MentorConfirmedAt, styles);
            WriteLink(row, 12, meeting.EvidenceImageUrl, workbook, styles);
            WriteText(row, 13, meeting.EvidenceSubmittedByStudent?.FullName, styles.Body);
            WriteInstant(row, 14, meeting.EvidenceSubmittedAt, styles);
            WriteInstant(row, 15, meeting.CompletedAt, styles);
            WriteInstant(row, 16, meeting.CanceledAt, styles);
            WriteText(row, 17, meeting.CancelReason, styles.Wrapped);
        }
        FinishSheet(sheet, rowIndex - 1, DetailHeaders.Length - 1);
    }

    private static Styles CreateStyles(IWorkbook workbook)
    {
        var headerFont = workbook.CreateFont();
        headerFont.IsBold = true;
        headerFont.Color = IndexedColors.White.Index;
        var header = workbook.CreateCellStyle();
        header.SetFont(headerFont);
        header.FillForegroundColor = IndexedColors.Orange.Index;
        header.FillPattern = FillPattern.SolidForeground;
        header.Alignment = HorizontalAlignment.Center;
        header.VerticalAlignment = VerticalAlignment.Center;
        header.WrapText = true;
        AddBorders(header);
        var body = workbook.CreateCellStyle();
        body.VerticalAlignment = VerticalAlignment.Top;
        AddBorders(body);
        var wrapped = workbook.CreateCellStyle();
        wrapped.CloneStyleFrom(body);
        wrapped.WrapText = true;
        var dateTime = workbook.CreateCellStyle();
        dateTime.CloneStyleFrom(body);
        dateTime.DataFormat = workbook.CreateDataFormat().GetFormat("dd/mm/yyyy hh:mm");
        var linkFont = workbook.CreateFont();
        linkFont.Color = IndexedColors.Blue.Index;
        linkFont.Underline = FontUnderlineType.Single;
        var hyperlink = workbook.CreateCellStyle();
        hyperlink.CloneStyleFrom(body);
        hyperlink.SetFont(linkFont);
        return new Styles(header, body, wrapped, dateTime, hyperlink);
    }

    private static void WriteHeader(ISheet sheet, Styles styles, IEnumerable<string> labels)
    {
        var row = sheet.CreateRow(0);
        row.HeightInPoints = 32;
        var column = 0;
        foreach (var label in labels)
        {
            var cell = row.CreateCell(column++);
            cell.SetCellValue(label);
            cell.CellStyle = styles.Header;
        }
        sheet.CreateFreezePane(0, 1);
    }

    private static void WriteText(IRow row, int column, string? value, ICellStyle style)
    {
        var cell = row.CreateCell(column);
        cell.SetCellValue(value ?? string.Empty);
        cell.CellStyle = style;
    }

    private static void WriteNumber(IRow row, int column, long value, ICellStyle style)
    {
        var cell = row.CreateCell(column);
        cell.SetCellValue(value);
        cell.CellStyle = style;
    }

    private static void WriteInstant(IRow row, int column, DateTime? value, Styles styles)
    {
        var cell = row.CreateCell(column);
        if (value.HasValue)
        {
            var utc = DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
            cell.SetCellValue(TimeZoneInfo.ConvertTimeFromUtc(utc, BusinessZone));
        }
        cell.CellStyle = styles.DateTime;
    }

    private static void WriteLink(IRow row, int column, string? value, IWorkbook workbook, Styles styles)
    {
        var cell = row.CreateCell(column);
        cell.SetCellValue(value ?? string.Empty);
        cell.CellStyle = string.IsNullOrWhiteSpace(value) ? styles.Body : styles.Hyperlink;
        if (!string.IsNullOrWhiteSpace(value))
        {
            var link = workbook.GetCreationHelper().CreateHyperlink(HyperlinkType.Url);
            link.Address = value;
            cell.Hyperlink = link;
        }
    }

    private static void FinishSheet(ISheet sheet, int lastRow, int lastColumn)
    {
        sheet.SetAutoFilter(new CellRangeAddress(0, Math.Max(lastRow, 0), 0, lastColumn));
        for (var column = 0; column <= lastColumn; column++)
        {
            sheet.AutoSizeColumn(column);
            sheet.SetColumnWidth(column, Math.Min(sheet.GetColumnWidth(column) + 512, 60 * 256));
        }
    }

    private static void AddBorders(ICellStyle style)
    {
        style.BorderTop = BorderStyle.Thin;
        style.BorderRight = BorderStyle.Thin;
        style.BorderBottom = BorderStyle.Thin;
        style.BorderLeft = BorderStyle.Thin;
        style.TopBorderColor = IndexedColors.Grey25Percent.Index;
        style.RightBorderColor = IndexedColors.Grey25Percent.Index;
        style.BottomBorderColor = IndexedColors.Grey25Percent.Index;
        style.LeftBorderColor = IndexedColors.Grey25Percent.Index;
    }

    private sealed record Styles(ICellStyle Header, ICellStyle Body, ICellStyle Wrapped,
        ICellStyle DateTime, ICellStyle Hyperlink);
}
