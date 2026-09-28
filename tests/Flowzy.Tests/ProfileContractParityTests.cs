using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Flowzy.Service.Authentication;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Flowzy.Tests;

public sealed class ProfileContractParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    private async Task<(HttpClient Client, long Id, string Email, string Code)> User(string role, bool profile = true)
    {
        using var bootstrap = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var code = Guid.NewGuid().ToString("N");
        var account = new Account { Email = code + "@local.test", PasswordHash = "unused", Role = role, Status = "ACTIVE" };
        if (profile)
        {
            if (role == "STUDENT") account.Student = new Student { StudentCode = code, FullName = "Original", Phone = "123", Status = "ACTIVE" };
            if (role == "MENTOR") account.Mentor = new Mentor { MentorCode = code, FullName = "Original", Status = "ACTIVE" };
            if (role == "INSTRUCTOR") account.Instructor = new Instructor { InstructorCode = code, FullName = "Original", Status = "ACTIVE" };
        }
        db.Accounts.Add(account); await db.SaveChangesAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(account));
        return (client, account.Id, account.Email, code);
    }

    private static async Task<JsonElement> Body(HttpResponseMessage response, HttpStatusCode status)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(status, body);
        using var json = JsonDocument.Parse(body); return json.RootElement.Clone();
    }

    [Theory]
    [InlineData("fullName", 255, "Full name")]
    [InlineData("phone", 30, "Phone")]
    [InlineData("major", 150, "Major")]
    [InlineData("cohort", 50, "Cohort")]
    [InlineData("className", 100, "Class name")]
    [InlineData("jobTitle", 150, "Job title")]
    [InlineData("company", 150, "Company")]
    [InlineData("linkedinUrl", 500, "LinkedIn URL")]
    [InlineData("department", 150, "Department")]
    public async Task SelfProfileRejectsOverlongFieldsBeforePersistence(string field, int max, string label)
    {
        var user = await User("STUDENT"); using var client = user.Client;
        var body = await Body(await client.PatchAsJsonAsync("/api/profile/me", new Dictionary<string, object> { [field] = new string('x', max + 1), ["address"] = "must not save" }), HttpStatusCode.BadRequest);
        body.GetProperty("message").GetString().Should().Be("Validation failed");
        body.GetProperty("data").GetProperty(field).GetString().Should().Be($"{label} must not exceed {max} characters");
        var saved = (await Body(await client.GetAsync("/api/profile/me"), HttpStatusCode.OK)).GetProperty("data").GetProperty("studentProfile");
        saved.GetProperty("fullName").GetString().Should().Be("Original");
        saved.GetProperty("address").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task SelfProfileRejectsNegativeExperienceAndUnknownGender()
    {
        var user = await User("MENTOR"); using var client = user.Client;
        var invalid = await Body(await client.PatchAsJsonAsync("/api/profile/me", new { yearsOfExperience = -1 }), HttpStatusCode.BadRequest);
        invalid.GetProperty("data").GetProperty("yearsOfExperience").GetString().Should().Be("Years of experience must be at least 0");
        foreach (var gender in new[] { "UNKNOWN", "male" })
        {
            var body = await Body(await client.PatchAsJsonAsync("/api/profile/me", new { gender }), HttpStatusCode.BadRequest);
            body.GetProperty("message").GetString().Should().Be("Malformed JSON request");
        }
        await Body(await client.PatchAsJsonAsync("/api/profile/me", new { yearsOfExperience = 0 }), HttpStatusCode.OK);
    }

    [Fact]
    public async Task SelfProfileTrimsClearsAndIgnoresProtectedFields()
    {
        var user = await User("STUDENT"); using var client = user.Client;
        var body = await Body(await client.PatchAsJsonAsync("/api/profile/me", new { fullName = "  Updated  ", phone = "  ", gender = "FEMALE", dateOfBirth = "2000-01-02", email = "wrong@local.test", studentCode = "wrong", role = "ADMIN", status = "INACTIVE" }), HttpStatusCode.OK);
        var data = body.GetProperty("data"); var profile = data.GetProperty("studentProfile");
        data.GetProperty("email").GetString().Should().Be(user.Email);
        data.GetProperty("role").GetString().Should().Be("STUDENT"); data.GetProperty("status").GetString().Should().Be("ACTIVE");
        profile.GetProperty("studentCode").GetString().Should().Be(user.Code);
        profile.GetProperty("fullName").GetString().Should().Be("Updated"); profile.GetProperty("phone").ValueKind.Should().Be(JsonValueKind.Null);
        profile.GetProperty("gender").GetString().Should().Be("FEMALE"); profile.GetProperty("dateOfBirth").GetString().Should().Be("2000-01-02");
        await Body(await client.PatchAsJsonAsync("/api/profile/me", new { fullName = (string?)null }), HttpStatusCode.OK);
        var blank = await Body(await client.PatchAsJsonAsync("/api/profile/me", new { fullName = " " }), HttpStatusCode.BadRequest);
        blank.GetProperty("message").GetString().Should().Be("Full name cannot be blank");
    }

    [Theory]
    [InlineData("ADMIN", 403, "Admins do not have an editable role profile")]
    [InlineData("STUDENT", 404, "Student profile not found")]
    [InlineData("MENTOR", 404, "Mentor profile not found")]
    [InlineData("INSTRUCTOR", 404, "Instructor profile not found")]
    public async Task RoleAndMissingProfileErrorsTakePrecedenceOverBlankName(string role, int status, string message)
    {
        var user = await User(role, false); using var client = user.Client;
        var body = await Body(await client.PatchAsJsonAsync("/api/profile/me", new { fullName = " " }), (HttpStatusCode)status);
        body.GetProperty("message").GetString().Should().Be(message);
    }

    [Theory]
    [InlineData("STUDENT", "Mentor or Instructor profiles are not allowed for STUDENT role")]
    [InlineData("MENTOR", "Student or Instructor profiles are not allowed for MENTOR role")]
    [InlineData("INSTRUCTOR", "Student or Mentor profiles are not allowed for INSTRUCTOR role")]
    [InlineData("ADMIN", "Profiles are not allowed for ADMIN role")]
    public async Task AdminCreateAndUpdateRejectMismatchedProfilesWithoutChangingData(string role, string message)
    {
        var admin = await User("ADMIN"); using var client = admin.Client;
        var target = await User(role); using var targetClient = target.Client;
        var email = Guid.NewGuid().ToString("N") + "@local.test";
        var request = new { email, role, initialPassword = "Password123", status = "ACTIVE", mustChangePassword = true,
            studentProfile = new { studentCode = target.Code, fullName = "Modified" },
            mentorProfile = new { mentorCode = target.Code, fullName = "Modified" },
            instructorProfile = new { instructorCode = target.Code, fullName = "Modified" } };
        var created = await Body(await client.PostAsJsonAsync("/api/admin/users", request), HttpStatusCode.BadRequest);
        created.GetProperty("message").GetString().Should().Be(message);
        var updated = await Body(await client.PatchAsJsonAsync($"/api/admin/users/{target.Id}", request), HttpStatusCode.BadRequest);
        updated.GetProperty("message").GetString().Should().Be(message);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        (await db.Accounts.AnyAsync(x => x.Email == email)).Should().BeFalse();
        (await db.Accounts.SingleAsync(x => x.Id == target.Id)).Email.Should().Be(target.Email);
    }

    [Theory]
    [InlineData("STUDENT", "Student")]
    [InlineData("MENTOR", "Mentor")]
    [InlineData("INSTRUCTOR", "Instructor")]
    public async Task AdminCreateRequiresMatchingProfile(string role, string label)
    {
        var admin = await User("ADMIN"); using var client = admin.Client;
        var body = await Body(await client.PostAsJsonAsync("/api/admin/users", new { email = Guid.NewGuid().ToString("N") + "@local.test", role, initialPassword = "Password123" }), HttpStatusCode.BadRequest);
        body.GetProperty("message").GetString().Should().Be($"{label} profile details are required for {role} role");
    }
}
