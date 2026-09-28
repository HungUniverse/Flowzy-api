using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Flowzy.Service.Authentication;
using Flowzy.Service.Backup;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace Flowzy.Tests;

public sealed class BackupApiFactory : FlowzyApiFactory
{
    public string BackupDirectory { get; } = Path.Combine(Path.GetTempPath(), "flowzy-backup-tests", Guid.NewGuid().ToString("N"));
    public ContainerBackupProcess Process { get; private set; } = null!;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        { ["Backup:Directory"] = BackupDirectory, ["Backup:SchedulerEnabled"] = "false" }));
        builder.ConfigureServices(services =>
        {
            Process = new(DatabaseContainer);
            services.RemoveAll<IPostgresBackupProcess>(); services.AddSingleton<IPostgresBackupProcess>(Process);
        });
    }
}

// Executes real PostgreSQL 16 dump/restore against this fixture's disposable container,
// never against the local project database or an externally supplied connection string.
public sealed class ContainerBackupProcess(PostgreSqlContainer postgres) : IPostgresBackupProcess
{
    public string? DumpError { get; set; }
    public TaskCompletionSource? DumpStarted { get; set; }
    public TaskCompletionSource? DumpRelease { get; set; }
    public TaskCompletionSource? RestoreStarted { get; set; }
    public TaskCompletionSource? RestoreRelease { get; set; }
    public async Task Dump(string output, CancellationToken ct)
    {
        DumpStarted?.TrySetResult(); if (DumpRelease is not null) await DumpRelease.Task.WaitAsync(ct);
        if (DumpError is not null) throw new InvalidOperationException(DumpError);
        var file = "/tmp/" + Guid.NewGuid().ToString("N") + ".dump";
        var result = await postgres.ExecAsync(["pg_dump", "--format=custom", "--blobs", "--no-owner", "--no-acl", "--username", "flowzy_test", "--dbname", "flowzy_test", "--file", file], ct);
        if (result.ExitCode != 0) throw new InvalidOperationException(result.Stderr);
        await File.WriteAllBytesAsync(output, await postgres.ReadFileAsync(file, ct), ct);
    }
    public async Task Restore(string input, CancellationToken ct)
    {
        RestoreStarted?.TrySetResult(); if (RestoreRelease is not null) await RestoreRelease.Task.WaitAsync(ct);
        var file = "/tmp/" + Guid.NewGuid().ToString("N") + ".dump";
        await postgres.CopyAsync(await File.ReadAllBytesAsync(input, ct), file, ct: ct);
        var result = await postgres.ExecAsync(["pg_restore", "--clean", "--if-exists", "--no-owner", "--no-acl", "--single-transaction", "--exit-on-error", "--username", "flowzy_test", "--dbname", "flowzy_test", file], ct);
        if (result.ExitCode != 0) throw new InvalidOperationException("pg_restore exited with code " + result.ExitCode + ": " + result.Stderr);
    }
}

public sealed class BackupParityTests(BackupApiFactory factory) : IClassFixture<BackupApiFactory>
{
    [Fact]
    public async Task ManualDumpDownloadsAndRestoresOnlyTheDisposableDatabase()
    {
        using var admin = await Admin();
        var termCode = "BK" + Guid.NewGuid().ToString("N")[..10];
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            db.AcademicTerms.Add(new() { Code = termCode, Status = "OPEN" }); await db.SaveChangesAsync();
        }
        var job = await Data(await admin.PostAsync("/api/admin/backups", null), HttpStatusCode.OK);
        job.GetProperty("status").GetString().Should().Be("QUEUED"); var id = job.GetProperty("id").GetInt64();
        var done = await Wait(admin, id); done.GetProperty("status").GetString().Should().Be("SUCCEEDED");
        var download = await admin.GetAsync($"/api/admin/backups/{id}/download"); download.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Content.Headers.ContentDisposition!.ToString().Should().Be("attachment; filename=\"" + done.GetProperty("fileName").GetString() + "\"");
        var bytes = await download.Content.ReadAsByteArrayAsync(); System.Text.Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("PGDMP");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            var term = await db.AcademicTerms.SingleAsync(x => x.Code == termCode); term.Status = "CLOSED"; await db.SaveChangesAsync();
        }
        using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent(bytes), "file", "database.dump"); form.Add(new StringContent("RESTORE_DATABASE"), "confirmation");
        var restored = await Data(await admin.PostAsync("/api/admin/backups/restore", form), HttpStatusCode.OK);
        restored.GetProperty("fileName").GetString().Should().StartWith("restore-upload-");
        await using var check = factory.Services.CreateAsyncScope(); var store = check.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        (await store.AcademicTerms.SingleAsync(x => x.Code == termCode)).Status.Should().Be("OPEN");
        var historicalJob = await store.BackupJobs.SingleAsync(x => x.Id == id);
        historicalJob.Status.Should().Be("FAILED"); historicalJob.ErrorMessage.Should().Be("Marked failed after database restore because this job state came from the restored dump.");
    }

    [Fact]
    public async Task ConcurrentRequestsCannotQueueTwoBackupsAndFailuresArePersisted()
    {
        using var admin = await Admin(); factory.Process.DumpStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Process.DumpRelease = new(TaskCreationOptions.RunContinuationsAsynchronously); factory.Process.DumpError = new string('x', 4500);
        try
        {
            var responses = await Task.WhenAll(admin.PostAsync("/api/admin/backups", null), admin.PostAsync("/api/admin/backups", null));
            responses.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
            var queued = await Data(responses.Single(x => x.StatusCode == HttpStatusCode.OK), HttpStatusCode.OK); var id = queued.GetProperty("id").GetInt64();
            await factory.Process.DumpStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            (await admin.GetAsync($"/api/admin/backups/{id}/download")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            factory.Process.DumpRelease.TrySetResult(); var failed = await Wait(admin, id);
            failed.GetProperty("status").GetString().Should().Be("FAILED"); failed.GetProperty("errorMessage").GetString()!.Length.Should().Be(4000);
        }
        finally { factory.Process.DumpRelease.TrySetResult(); factory.Process.DumpStarted = null; factory.Process.DumpRelease = null; factory.Process.DumpError = null; }
    }

    [Fact]
    public async Task DueScheduleTriggersOnceAndRetentionOnlyRemovesOwnedExpiredDumps()
    {
        using var admin = await Admin();
        await Data(await admin.PutAsJsonAsync("/api/admin/backups/schedule", new { enabled = true, cronExpression = "0 0 2 * * *", timezone = "Asia/Ho_Chi_Minh", retentionDays = 1 }), HttpStatusCode.OK);
        Directory.CreateDirectory(factory.BackupDirectory);
        var expired = Path.Combine(factory.BackupDirectory, "fspark-postgres-expired.dump");
        var other = Path.Combine(factory.BackupDirectory, "restore-upload-keep.dump");
        await File.WriteAllTextAsync(expired, "test"); await File.WriteAllTextAsync(other, "test");
        File.SetLastWriteTimeUtc(expired, DateTime.UtcNow.AddDays(-3)); File.SetLastWriteTimeUtc(other, DateTime.UtcNow.AddDays(-3));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>(); var settings = await db.BackupScheduleSettings.SingleAsync();
            settings.NextRunAt = DateTime.UtcNow.AddMinutes(-1); await db.SaveChangesAsync();
        }
        await Task.WhenAll(Trigger(), Trigger());
        var page = await Data(await admin.GetAsync("/api/admin/backups"), HttpStatusCode.OK);
        var job = page.GetProperty("content")[0]; job.GetProperty("triggerType").GetString().Should().Be("SCHEDULED"); job.GetProperty("requestedByAccountId").ValueKind.Should().Be(JsonValueKind.Null);
        (await Wait(admin, job.GetProperty("id").GetInt64())).GetProperty("status").GetString().Should().Be("SUCCEEDED");
        // Cleanup follows the success write; allow the worker to finish the final filesystem step.
        for (var i = 0; i < 50 && File.Exists(expired); i++) await Task.Delay(20);
        File.Exists(expired).Should().BeFalse(); File.Exists(other).Should().BeTrue();
        var schedule = await Data(await admin.GetAsync("/api/admin/backups/schedule"), HttpStatusCode.OK);
        schedule.GetProperty("lastTriggeredAt").ValueKind.Should().NotBe(JsonValueKind.Null);
        var next = schedule.GetProperty("nextRunAt").GetDateTime(); next.Hour.Should().Be(19); next.Should().BeAfter(DateTime.UtcNow);
        await Data(await admin.PutAsJsonAsync("/api/admin/backups/schedule", new { enabled = false, cronExpression = "@daily", timezone = "UTC" }), HttpStatusCode.OK);
        async Task Trigger() { await using var scope = factory.Services.CreateAsyncScope(); await scope.ServiceProvider.GetRequiredService<IBackupService>().RunDueSchedule(default); }
    }

    [Fact]
    public async Task RestoreFailureReleasesGateAndConcurrentOperationsAreRejected()
    {
        using var admin = await Admin(); factory.Process.RestoreStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Process.RestoreRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var form = RestoreForm();
        var restore = admin.PostAsync("/api/admin/backups/restore", form);
        try
        {
            await factory.Process.RestoreStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var backup = await admin.PostAsync("/api/admin/backups", null); backup.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await backup.Content.ReadAsStringAsync()).Should().Contain("A database restore is already running");
            using var second = RestoreForm(); (await admin.PostAsync("/api/admin/backups/restore", second)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
        finally { factory.Process.RestoreRelease.TrySetResult(); }
        var result = await restore; result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await result.Content.ReadAsStringAsync()).Should().Contain("Database restore failed:");
        factory.Services.GetRequiredService<BackupOperationGate>().RestoreInProgress.Should().BeFalse();
        factory.Process.RestoreStarted = null; factory.Process.RestoreRelease = null;
        static MultipartFormDataContent RestoreForm()
        {
            var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent([1, 2, 3]), "file", "invalid.dump");
            form.Add(new StringContent("RESTORE_DATABASE"), "confirmation"); return form;
        }
    }

    [Fact]
    public async Task SchedulerReschedulesMissingOrBusyRunWithoutCreatingDuplicateJob()
    {
        using var admin = await Admin();
        await Data(await admin.PutAsJsonAsync("/api/admin/backups/schedule", new { enabled = true, cronExpression = "*/30 * * * * *", timezone = "UTC" }), HttpStatusCode.OK);
        long before;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>(); before = await db.BackupJobs.LongCountAsync();
            var settings = await db.BackupScheduleSettings.SingleAsync(); settings.NextRunAt = null; await db.SaveChangesAsync();
        }
        await Trigger();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>(); (await db.BackupJobs.LongCountAsync()).Should().Be(before);
            var settings = await db.BackupScheduleSettings.SingleAsync(); settings.NextRunAt.Should().BeAfter(DateTime.UtcNow);
            settings.NextRunAt = DateTime.UtcNow.AddMinutes(-1);
            db.BackupJobs.Add(new() { TriggerType = "MANUAL", Status = "RUNNING", CreatedAt = DateTime.UtcNow }); await db.SaveChangesAsync();
        }
        await Trigger();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>(); (await db.BackupJobs.LongCountAsync()).Should().Be(before + 1);
            (await db.BackupScheduleSettings.SingleAsync()).NextRunAt.Should().BeAfter(DateTime.UtcNow);
            await scope.ServiceProvider.GetRequiredService<IBackupService>().FailInterrupted(default);
        }
        await Data(await admin.PutAsJsonAsync("/api/admin/backups/schedule", new { enabled = false, cronExpression = "@daily", timezone = "UTC" }), HttpStatusCode.OK);
        async Task Trigger() { await using var scope = factory.Services.CreateAsyncScope(); await scope.ServiceProvider.GetRequiredService<IBackupService>().RunDueSchedule(default); }
    }

    [Fact]
    public async Task ValidationRecoveryAndAuthorizationMatchBackupContract()
    {
        using var admin = await Admin(); using var anonymous = factory.CreateClient();
        (await anonymous.GetAsync("/api/admin/backups")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        foreach (var path in new[] { "?page=-1", "?size=101", "?status=unknown" })
            (await admin.GetAsync("/api/admin/backups" + path)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await (await admin.GetAsync("/api/admin/backups?status=unknown")).Content.ReadAsStringAsync()).Should().Contain("Invalid parameter format: status");
        foreach (var days in new[] { 0, 3651 })
        {
            var invalidRetention = await admin.PutAsJsonAsync("/api/admin/backups/schedule", new { enabled = false, cronExpression = "@daily", timezone = "UTC", retentionDays = days });
            invalidRetention.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await invalidRetention.Content.ReadAsStringAsync()).Should().Contain(days == 0 ? "Retention days must be at least 1" : "Retention days must not exceed 3650");
        }
        var missing = await admin.GetAsync("/api/admin/backups/9223372036854775807"); missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await missing.Content.ReadAsStringAsync()).Should().Contain("Backup job not found with id: 9223372036854775807");
        var enabledMissing = await admin.PutAsJsonAsync("/api/admin/backups/schedule", new { cronExpression = "0 0 2 * * *", timezone = "UTC" });
        enabledMissing.StatusCode.Should().Be(HttpStatusCode.BadRequest); (await enabledMissing.Content.ReadAsStringAsync()).Should().Contain("Enabled flag is required");
        (await admin.PutAsJsonAsync("/api/admin/backups/schedule", new { enabled = true, cronExpression = "invalid cron fields a b c", timezone = "UTC" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PutAsJsonAsync("/api/admin/backups/schedule", new { enabled = true, cronExpression = "0 0 2 * * *", timezone = "Not/AZone" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var jobs = new[] { new BackupJob { TriggerType = "MANUAL", Status = "QUEUED", CreatedAt = DateTime.UtcNow }, new BackupJob { TriggerType = "MANUAL", Status = "RUNNING", CreatedAt = DateTime.UtcNow } };
        db.BackupJobs.AddRange(jobs); await db.SaveChangesAsync();
        await scope.ServiceProvider.GetRequiredService<IBackupService>().FailInterrupted(default);
        (await db.BackupJobs.Where(x => jobs.Select(j => j.Id).Contains(x.Id)).ToListAsync()).Should().OnlyContain(x => x.Status == "FAILED" && x.ErrorMessage == "Marked failed on backend startup because the previous backup worker is no longer running.");
    }

    private async Task<HttpClient> Admin()
    {
        var client = factory.CreateClient(); await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>(); var account = await db.Accounts.SingleAsync(x => x.Email == "admin.integration@local.test");
        account.MustChangePassword = false; await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(account)); return client;
    }
    private static async Task<JsonElement> Wait(HttpClient client, long id)
    {
        for (var i = 0; i < 200; i++)
        {
            var result = await Data(await client.GetAsync($"/api/admin/backups/{id}"), HttpStatusCode.OK);
            if (result.GetProperty("status").GetString() is "SUCCEEDED" or "FAILED") return result;
            await Task.Delay(50);
        }
        throw new TimeoutException("Backup worker did not finish");
    }
    private static async Task<JsonElement> Data(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync(); response.StatusCode.Should().Be(expected, body);
        using var document = JsonDocument.Parse(body); return document.RootElement.GetProperty("data").Clone();
    }
}
