using Flowzy.Service.Backup;
using Flowzy.Service.Exceptions;
using FluentAssertions;
using Xunit;

namespace Flowzy.Tests;

public sealed class BackupCronParityTests
{
    // Expected values also evaluated using the Java oracle's Spring CronExpression.
    [Theory]
    [InlineData("0 30 2 * * *", "Europe/Berlin", "2026-03-29T00:00:00Z", "2026-03-30T00:30:00Z")]
    [InlineData("0 30 2 * * *", "Europe/Berlin", "2026-10-25T00:45:00Z", "2026-10-25T01:30:00Z")]
    [InlineData("0 0,50 2 * * *", "Europe/Berlin", "2026-10-25T00:45:00Z", "2026-10-25T00:50:00Z")]
    [InlineData("0 0 2 * * *", "Asia/Ho_Chi_Minh", "2026-09-24T00:00:00Z", "2026-09-24T19:00:00Z")]
    [InlineData("0 0 0 L * *", "UTC", "2026-09-24T00:00:00Z", "2026-09-30T00:00:00Z")]
    [InlineData("0 0 0 * * MON#2", "UTC", "2026-09-24T00:00:00Z", "2026-10-12T00:00:00Z")]
    [InlineData("@daily", "+18:00", "2026-09-24T00:00:00Z", "2026-09-24T06:00:00Z")]
    [InlineData("@daily", "UTC+7", "2026-09-24T00:00:00Z", "2026-09-24T17:00:00Z")]
    [InlineData("0 0 0 L,15 * *", "UTC", "2026-09-24T00:00:00Z", "2026-09-30T00:00:00Z")]
    [InlineData("0 0 0 * * MONL,FRIL", "UTC", "2026-09-24T00:00:00Z", "2026-09-25T00:00:00Z")]
    public void NextRunMatchesJava(string cron, string zone, string from, string expected) =>
        BackupCron.Parse(cron).Next(DateTimeOffset.Parse(from).UtcDateTime, BackupCron.Zone(zone)).Should().Be(DateTimeOffset.Parse(expected).UtcDateTime);

    [Theory]
    [InlineData("* * * * *")]
    [InlineData("99 0 0 * * *")]
    [InlineData("0 0 23-2 * * *")]
    [InlineData("? 0 0 * * *")]
    [InlineData("@every_second")]
    [InlineData("0 0 0 * DEC-FEB *")]
    [InlineData("0 0 0 L-5W * *")]
    public void InvalidCronIsRejected(string cron)
    {
        var act = () => BackupCron.Parse(cron); act.Should().Throw<BadRequestException>().WithMessage("Invalid cron expression");
    }

    [Fact]
    public void ImpossibleCronHasJavaError()
    {
        var act = () => BackupCron.Parse("0 0 0 31 2 *").Next(DateTime.UtcNow, BackupCron.Zone("UTC"));
        act.Should().Throw<BadRequestException>().WithMessage("Cron expression does not produce a next run time");
    }
}
