using System.Globalization;
using System.Text;
using Flowzy.Service.Exceptions;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace Flowzy.Service.Grades;

public sealed partial class GradeMatrixService
{
    public async Task<byte[]> Export(string? term, string? course, long? gid, string email, bool xlsx, CancellationToken ct)
    {
        var instructor = await Instructor(email, false, ct); term = Optional(term); course = Optional(course);
        var groups = (await repository.AssignedGroups(instructor.Id, term, course, ct)).Where(x => gid is null || x.Id == gid).ToList();
        if (gid is not null && groups.Count == 0) throw new NotFoundException("Group not found for this instructor scope");
        var milestones = await Visible(instructor.Id, term, course, ct);
        if (!xlsx)
        {
            var csv = new StringBuilder("groupId,groupNo,groupName,studentId,studentCode,studentName");
            foreach (var m in milestones) csv.Append(',').Append(Csv(m.Title)).Append(" contribution %").Append(',').Append(Csv(m.Title)).Append(" individual score");
            csv.Append(",finalTotal,complete\r\n");
            foreach (var group in groups)
                foreach (var member in (await Build(group, ct)).Members)
                {
                    csv.Append(group.Id.ToString(CultureInfo.InvariantCulture)).Append(',').Append(Csv(group.GroupNo)).Append(',').Append(Csv(group.Name)).Append(',')
                        .Append(member.StudentId.ToString(CultureInfo.InvariantCulture)).Append(',').Append(Csv(member.StudentCode)).Append(',').Append(Csv(member.StudentName));
                    foreach (var m in milestones)
                    {
                        var score = member.MilestoneScores.FirstOrDefault(x => x.MilestoneId == m.Id);
                        csv.Append(',').Append(score?.ContributionPercent is { } contribution ? Number(contribution) : "").Append(',').Append(score?.CalculatedScore is { } calculated ? Number(calculated) : "");
                    }
                    csv.Append(',').Append(member.TotalScore.ToString("0.0000", CultureInfo.InvariantCulture)).Append(',').Append(member.Complete ? "true" : "false").Append("\r\n");
                }
            return Encoding.UTF8.GetBytes(csv.ToString());
        }
        using var workbook = new XSSFWorkbook(); var sheet = workbook.CreateSheet("RAW"); sheet.CreateFreezePane(0, 1);
        var headerStyle = workbook.CreateCellStyle(); var font = workbook.CreateFont(); font.IsBold = true; headerStyle.SetFont(font);
        var scoreStyle = workbook.CreateCellStyle(); scoreStyle.DataFormat = workbook.CreateDataFormat().GetFormat("0.0###");
        var headers = new[] { "Tên Nhóm", "Họ & Tên", "MSSV", "Email (đuôi FPT)" }.Concat(milestones.Select(x => x.Title)).Append("Final").ToArray();
        var header = sheet.CreateRow(0);
        for (var column = 0; column < headers.Length; column++) { var cell = header.CreateCell(column); cell.SetCellValue(headers[column]); cell.CellStyle = headerStyle; }
        var rowIndex = 1;
        foreach (var group in groups)
        {
            var matrix = await Build(group, ct); var members = await ActiveMembers(group.Id, ct);
            foreach (var member in matrix.Members)
            {
                var row = sheet.CreateRow(rowIndex++); row.CreateCell(0).SetCellValue(group.Name); row.CreateCell(1).SetCellValue(member.StudentName);
                row.CreateCell(2).SetCellValue(member.StudentCode); row.CreateCell(3).SetCellValue(members.First(x => x.StudentId == member.StudentId).Student.Account.Email);
                for (var col = 0; col < milestones.Count; col++)
                    if (member.MilestoneScores.FirstOrDefault(x => x.MilestoneId == milestones[col].Id)?.CalculatedScore is { } value) CellNumber(row, col + 4, value, scoreStyle);
                if (member.Complete) CellNumber(row, milestones.Count + 4, member.TotalScore, scoreStyle);
            }
        }
        sheet.SetAutoFilter(new CellRangeAddress(0, Math.Max(rowIndex - 1, 0), 0, headers.Length - 1));
        for (var col = 0; col < headers.Length; col++) sheet.AutoSizeColumn(col);
        using var output = new MemoryStream(); workbook.Write(output, true); return output.ToArray();
    }
    private static void CellNumber(IRow row, int column, decimal value, ICellStyle style)
    { var cell = row.CreateCell(column); cell.SetCellValue((double)value); cell.CellStyle = style; }
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Csv(string? value) => value is null ? "" : value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? '"' + value.Replace("\"", "\"\"") + '"' : value;
}
