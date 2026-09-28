using System.Text;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Imports;
using FluentAssertions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Xunit;

namespace Flowzy.Tests;

public sealed class ImportParserParityTests
{
    [Theory]
    [InlineData("2025-02-31", "2025-02-28")]
    [InlineData("2024-02-30", "2024-02-29")]
    [InlineData("01/02/2025", "2025-01-02")]
    [InlineData("31/01/2025", "2025-01-31")]
    [InlineData("2025/04/31", "2025-04-30")]
    [InlineData("2025-01-32", null)]
    [InlineData("2025-13-01", null)]
    [InlineData("2025-1-01", null)]
    public void DatesUseJavaSmartResolutionAndFormatPrecedence(string value, string? expected) =>
        (ImportValidation.Date(value)?.ToString("yyyy-MM-dd")).Should().Be(expected);

    [Theory]
    [InlineData("RollNumber,Fullname,Email,SubjectCode", "The header row must contain exactly five columns")]
    [InlineData("RollNumber,Fullname,Email,SubjectCode,role", "Unsupported column: role")]
    [InlineData("RollNumber,Fullname,Email,SubjectCode,Email", "Duplicate column: Email")]
    [InlineData("RollNumber,Fullname,Email,SubjectCode,", "Missing required columns: group_name")]
    public void RosterRejectsUnsupportedHeaderLayout(string header, string detail)
    {
        var act = () => Csv(header + "\nSE1,Student,test@example.com,EXE101,G1", "STUDENT_ACCOUNT");
        act.Should().Throw<BadRequestException>().WithMessage("UNSUPPORTED_COLUMN: " + detail);
    }

    [Fact]
    public void CsvPreservesJavaRecordNumbersQuotedFieldsAndBomHeaders()
    {
        var rows = Csv("\uFEFF Email ,GroupName,Fullname,SubjectCode,RollNumber\n\n,, , ,\n student@example.com ,G1,\"Student, One\",EXE101, SE1\n", "STUDENT_ACCOUNT");
        rows.Should().ContainSingle(); rows[0].Number.Should().Be(3);
        rows[0].Value("full_name").Should().Be("Student, One"); rows[0].Value("roll_number").Should().Be("SE1");
    }

    [Fact]
    public void GenericCsvUsesVietnameseAliasesAndRejectsRoleColumn()
    {
        var rows = Csv("Import instructions\nMSSV,Họ tên,Email (Email trên FAP của SV),Ngành\nSE1,Nguyễn Văn An,an@example.com,CNTT", "STUDENT");
        rows[0].Number.Should().Be(3); rows[0].Value("full_name").Should().Be("Nguyễn Văn An"); rows[0].Value("major").Should().Be("CNTT");
        var act = () => Csv("student_code,full_name,email,role\nSE1,An,an@example.com,ADMIN", "STUDENT");
        act.Should().Throw<BadRequestException>().WithMessage("UNSUPPORTED_COLUMN");
    }

    [Fact]
    public void RosterReadsSupportedSheetsAndRetainsPhysicalRowNumbers()
    {
        using var workbook = new XSSFWorkbook(); SetRow(workbook.CreateSheet("Instructions"), 0, "Read this first");
        foreach (var name in new[] { "Class A", "Class B" })
        {
            var sheet = workbook.CreateSheet(name); SetRow(sheet, 2, "RollNumber", "Fullname", "Email", "SubjectCode", "GroupName");
            SetRow(sheet, 4, name, "Student", "student@example.com", "EXE101", "G1");
        }
        var rows = Xlsx(workbook, "STUDENT_ACCOUNT"); rows.Should().HaveCount(2);
        rows.Should().OnlyContain(x => x.Number == 5); rows.Select(x => x.Sheet).Should().Equal("Class A", "Class B");
        SetRow(workbook.CreateSheet("Malformed recognized sheet"), 0, "Email", "role");
        var act = () => Xlsx(workbook, "STUDENT_ACCOUNT"); act.Should().Throw<BadRequestException>().WithMessage("UNSUPPORTED_COLUMN: The header row must contain exactly five columns");
    }

    [Fact]
    public void GroupWorkbookInheritsGroupContextButFreeStudentsNeverInheritIt()
    {
        using var workbook = new XSSFWorkbook(); var main = workbook.CreateSheet("SU26_EXE101_Group_List");
        string[] header = ["STT", "Tên nhóm", "MSSV", "Họ tên", "Email", "Họ tên leader", "Mentor phân bổ"];
        SetRow(main, 4, header);
        SetRow(main, 5, "1", "Group One", "SE1", "An", "an@example.com", "An", "Mentor (M1)");
        SetRow(main, 6, "", "", "SE2", "Binh", "binh@example.com", "", "");
        var free = workbook.CreateSheet("SV tìm nhóm"); SetRow(free, 0, header);
        SetRow(free, 1, "2", "Must not create", "SE3", "Chi", "chi@example.com", "Chi", "M2");
        var rows = Xlsx(workbook, "STUDENT"); rows.Should().HaveCount(3);
        rows[1].Value("group_name").Should().Be("Group One"); rows[1].Value("leader_full_name").Should().Be("An");
        rows[1].Value("mentor_assignment").Should().Be("Mentor (M1)"); rows[2].Value("group_name").Should().BeEmpty();
        rows[2].Value("mentor_assignment").Should().BeEmpty();
    }

    private static IReadOnlyList<ImportRow> Csv(string text, string target)
    {
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(text)); return ImportFileParser.Parse(input, "CSV", target);
    }
    private static IReadOnlyList<ImportRow> Xlsx(IWorkbook workbook, string target)
    {
        using var output = new MemoryStream(); workbook.Write(output, true); output.Position = 0;
        return ImportFileParser.Parse(output, "XLSX", target);
    }
    private static void SetRow(ISheet sheet, int index, params string[] values)
    {
        var row = sheet.CreateRow(index); for (var i = 0; i < values.Length; i++) row.CreateCell(i).SetCellValue(values[i]);
    }
}
