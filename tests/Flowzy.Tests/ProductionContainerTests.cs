using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NPOI.XSSF.UserModel;
using Testcontainers.PostgreSql;
using Xunit;
using Xunit.Abstractions;

namespace Flowzy.Tests;

public sealed class ProductionImageFactAttribute : FactAttribute
{
    public ProductionImageFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FLOWZY_PRODUCTION_TEST_IMAGE")))
            Skip = "Build the production Docker image and set FLOWZY_PRODUCTION_TEST_IMAGE to run this test.";
    }
}

public sealed class ProductionContainerTests(ITestOutputHelper output)
{
    [ProductionImageFact, Trait("Category", "ProductionImage")]
    public async Task ProductionImageSupportsTlsMigrationsExportsRestartAndBackupRestore()
    {
        // All resources belong to this test. No application DB, volumes, or ports are reused.
        await using var network = new NetworkBuilder().Build();
        await network.CreateAsync();
        await using var uploads = new VolumeBuilder().Build();
        await using var backups = new VolumeBuilder().Build();
        await uploads.CreateAsync();
        await backups.CreateAsync();
        var (ca, certificate, key) = Certificates();
        using var caFile = new TemporaryCaFile(ca);
        await using var postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine")
            .WithDatabase("production_smoke").WithUsername("flowzy_test").WithPassword("DisposableDatabase123!")
            .WithNetwork(network).WithNetworkAliases("db")
            .WithResourceMapping(certificate, "/tmp/flowzy-server.crt")
            .WithResourceMapping(key, "/tmp/flowzy-server.key", UnixFileModes.UserRead | UnixFileModes.UserWrite)
            .WithCreateParameterModifier(parameters =>
            {
                // Replace, rather than append to, PostgreSqlBuilder's default '-c' arguments.
                parameters.Entrypoint = ["/bin/sh", "-c"];
                parameters.Cmd = ["chown postgres:postgres /tmp/flowzy-server.key && chmod 600 /tmp/flowzy-server.key && exec docker-entrypoint.sh postgres -c ssl=on -c ssl_cert_file=/tmp/flowzy-server.crt -c ssl_key_file=/tmp/flowzy-server.key"];
            })
            .Build();
        try { await postgres.StartAsync(); }
        catch
        {
            try { var logs = await postgres.GetLogsAsync(); output.WriteLine(logs.Stdout + logs.Stderr); }
            catch (InvalidOperationException) { /* Container creation failed before logs were available. */ }
            throw;
        }

        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = "db", Port = 5432, Database = "production_smoke", Username = "flowzy_test",
            Password = "DisposableDatabase123!", SslMode = SslMode.VerifyFull,
            RootCertificate = "/etc/flowzy/ca.crt", Timeout = 5
        };
        await using var api = new ContainerBuilder()
            .WithImage(Environment.GetEnvironmentVariable("FLOWZY_PRODUCTION_TEST_IMAGE")!)
            .WithNetwork(network).WithPortBinding(8080, true)
            .WithBindMount(caFile.Path, "/etc/flowzy/ca.crt", AccessMode.ReadOnly)
            .WithVolumeMount(uploads, "/app/uploads").WithVolumeMount(backups, "/app/backups")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            .WithEnvironment("ASPNETCORE_URLS", "http://+:8080")
            .WithEnvironment("ConnectionStrings__Default", connection.ConnectionString)
            .WithEnvironment("Jwt__SecretKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)))
            .WithEnvironment("Admin__Email", "admin.smoke@example.test")
            .WithEnvironment("Admin__Password", "ProductionSmoke123!")
            .WithEnvironment("Cors__AllowedOrigins", "https://app.example.test")
            .WithEnvironment("WebSocket__AllowedOrigins", "https://app.example.test")
            .WithEnvironment("AllowedHosts", "localhost;127.0.0.1")
            .WithEnvironment("Backup__Directory", "/app/backups")
            .WithEnvironment("Import__UploadDirectory", "/app/uploads")
            .WithCreateParameterModifier(parameters =>
            {
                parameters.User = "1654:1654";
                parameters.HostConfig.ReadonlyRootfs = true;
                parameters.HostConfig.Tmpfs = new Dictionary<string, string> { ["/tmp"] = "rw,noexec,nosuid,size=256m,mode=1777" };
                parameters.HostConfig.CapDrop = ["ALL"];
                parameters.HostConfig.SecurityOpt = ["no-new-privileges:true"];
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8080).ForPath("/actuator/health")))
            .Build();
        try { await api.StartAsync(); }
        catch
        {
            try { var logs = await api.GetLogsAsync(); output.WriteLine(logs.Stdout + logs.Stderr); }
            catch (InvalidOperationException) { /* Container creation failed before logs were available. */ }
            throw;
        }
        using var client = new HttpClient { BaseAddress = new Uri($"http://{api.Hostname}:{api.GetMappedPublicPort(8080)}"), Timeout = TimeSpan.FromSeconds(30) };
        await using var db = new FlowzyDbContext(new DbContextOptionsBuilder<FlowzyDbContext>().UseNpgsql(postgres.GetConnectionString()).Options);
        var admin = await db.Accounts.SingleAsync(x => x.Email == "admin.smoke@example.test");
        admin.MustChangePassword = false;
        var mentor = new Mentor
        {
            MentorCode = "SMOKE-M", FullName = "Mentor export smoke", Status = "ACTIVE",
            Account = new Account { Email = "mentor.smoke@example.test", Role = "MENTOR", Status = "ACTIVE", MustChangePassword = false, PasswordHash = admin.PasswordHash }
        };
        var instructor = new Instructor
        {
            InstructorCode = "SMOKE-I", FullName = "Instructor export smoke", Status = "ACTIVE",
            Account = new Account { Email = "instructor.smoke@example.test", Role = "INSTRUCTOR", Status = "ACTIVE", MustChangePassword = false, PasswordHash = admin.PasswordHash }
        };
        db.Mentors.Add(mentor); db.Instructors.Add(instructor);
        db.AcademicTerms.Add(new AcademicTerm { Code = "SMOKE26", Status = "OPEN" });
        await db.SaveChangesAsync();
        db.StudentGroups.Add(new StudentGroup { Term = "SMOKE26", CourseCode = "EXE101", GroupNo = "1", Name = "Smoke group", Status = "ACTIVE", MentorId = mentor.Id, InstructorId = instructor.Id });
        await db.SaveChangesAsync();

        await Login(client, "mentor.smoke@example.test");
        await AssertWorkbook(client, "/api/mentor/meeting-reports/export.xlsx?term=SMOKE26", "Group Summary");
        await Login(client, "instructor.smoke@example.test");
        await AssertWorkbook(client, "/api/instructor/grades/export.xlsx?term=SMOKE26&courseCode=EXE101", "RAW");
        var oldToken = await Login(client, "admin.smoke@example.test");
        await AssertWorkbook(client, "/api/admin/feedback/export.xlsx?term=SMOKE26", "Mentor Feedback");

        (await client.PostAsync("/api/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        await api.StopAsync();
        await api.StartAsync();
        using var restartedClient = new HttpClient { BaseAddress = new Uri($"http://{api.Hostname}:{api.GetMappedPublicPort(8080)}"), Timeout = TimeSpan.FromSeconds(30) };
        // Docker may assign another host port on start; internal API and DB ports stay unchanged.
        restartedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldToken);
        (await restartedClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var newToken = await Login(restartedClient, "admin.smoke@example.test");
        newToken.Should().NotBe(oldToken);

        // PostgresBackupProcess must pass Root Certificate to libpq for this to succeed.
        using var created = await restartedClient.PostAsync("/api/admin/backups", null);
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        using var jobJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = jobJson.RootElement.GetProperty("data").GetProperty("id").GetInt64();
        using var waitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        while (true)
        {
            using var job = JsonDocument.Parse(await restartedClient.GetStringAsync($"/api/admin/backups/{id}", waitTimeout.Token));
            var data = job.RootElement.GetProperty("data");
            var status = data.GetProperty("status").GetString();
            status.Should().NotBe("FAILED", data.ToString());
            if (status == "SUCCEEDED") break;
            await Task.Delay(250, waitTimeout.Token);
        }
        var dump = await restartedClient.GetByteArrayAsync($"/api/admin/backups/{id}/download");
        Encoding.ASCII.GetString(dump, 0, 5).Should().Be("PGDMP");
        await db.AcademicTerms.Where(x => x.Code == "SMOKE26").ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "CLOSED"));
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(dump), "file", "smoke.dump");
        form.Add(new StringContent("RESTORE_DATABASE"), "confirmation");
        using var restore = await restartedClient.PostAsync("/api/admin/backups/restore", form);
        restore.StatusCode.Should().Be(HttpStatusCode.OK, await restore.Content.ReadAsStringAsync());
        (await db.AcademicTerms.AsNoTracking().SingleAsync(x => x.Code == "SMOKE26")).Status.Should().Be("OPEN");

        restartedClient.DefaultRequestHeaders.Authorization = null;
        await postgres.StopAsync();
        (await restartedClient.GetAsync("/actuator/health")).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    private static async Task<string> Login(HttpClient client, string email)
    {
        client.DefaultRequestHeaders.Authorization = null;
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "ProductionSmoke123!" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return token;
    }

    private static async Task AssertWorkbook(HttpClient client, string path, string sheet)
    {
        using var response = await client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK, response.IsSuccessStatusCode ? "valid workbook download" : await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        using var bytes = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var workbook = new XSSFWorkbook(bytes);
        workbook.GetSheet(sheet).Should().NotBeNull();
    }

    private static (byte[] Ca, byte[] Certificate, byte[] Key) Certificates()
    {
        using var caKey = RSA.Create(2048);
        var request = new CertificateRequest("CN=Flowzy disposable test CA", caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        using var ca = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        using var key = RSA.Create(2048);
        var server = new CertificateRequest("CN=db", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder(); san.AddDnsName("db");
        server.CertificateExtensions.Add(san.Build());
        server.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        server.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        server.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, false));
        using var certificate = server.Create(ca, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(12), RandomNumberGenerator.GetBytes(16));
        return (Encoding.ASCII.GetBytes(ca.ExportCertificatePem()), Encoding.ASCII.GetBytes(certificate.ExportCertificatePem()), Encoding.ASCII.GetBytes(key.ExportPkcs8PrivateKeyPem()));
    }

    private sealed class TemporaryCaFile : IDisposable
    {
        private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("flowzy-production-ca-");
        public string Path { get; }
        public TemporaryCaFile(byte[] certificate)
        {
            Path = System.IO.Path.Combine(directory.FullName, "ca.crt");
            File.WriteAllBytes(Path, certificate);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(Path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
        }
        public void Dispose() { File.Delete(Path); directory.Delete(); }
    }
}
