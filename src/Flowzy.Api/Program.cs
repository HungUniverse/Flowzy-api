using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flowzy.Api.Middleware;
using Flowzy.Api.Compatibility;
using Flowzy.Api.WebSockets;
using Flowzy.Repository;
using Flowzy.Repository.Migrations;
using Flowzy.Repository.Repositories;
using Flowzy.Service;
using Flowzy.Service.Authentication;
using Flowzy.Service.Contracts;
using Flowzy.Service.Startup;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is required");
var secret = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey is required");

builder.Services.AddFlowzyRepository(connectionString);
builder.Services.AddFlowzyServices(builder.Configuration);
builder.Services.AddHostedService<Flowzy.Api.Workers.BackupWorker>();
builder.Services.AddHostedService<Flowzy.Api.Workers.ImportWorker>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        if (context.ModelState.Keys.Any(key => key.StartsWith('$') || key.Length == 0))
        {
            return new BadRequestObjectResult(ApiResponse<object>.Error(400, "Malformed JSON request"));
        }
        var errors = context.ModelState
            .Where(pair => pair.Value?.Errors.Count > 0)
            .ToDictionary(
                pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key),
                pair => pair.Value!.Errors.First().ErrorMessage);
        return new BadRequestObjectResult(ApiResponse<Dictionary<string, string>>.Error(
            400, "Validation failed", errors));
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Flowzy API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
});
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var header = context.HttpContext.Request.Headers.Authorization.ToString();
                var token = header.StartsWith("Bearer ", StringComparison.Ordinal) ? header[7..] : string.Empty;
                var blacklist = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                if (blacklist.IsBlacklisted(token))
                {
                    context.Fail("Token is blacklisted");
                    return;
                }
                var email = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var repository = context.HttpContext.RequestServices.GetRequiredService<IAccountRepository>();
                var account = email is null ? null : await repository.FindByEmailAsync(email, context.HttpContext.RequestAborted);
                if (account is null || account.Status != "ACTIVE")
                {
                    context.Fail("Account is not active");
                    return;
                }
                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    foreach (var claim in identity.FindAll("role").ToArray()) identity.RemoveClaim(claim);
                    identity.AddClaim(new Claim("role", account.Role));
                    identity.AddClaim(new Claim("mustChangePassword", account.MustChangePassword.ToString().ToLowerInvariant()));
                }
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    ApiResponse<object>.Error(401, "Unauthorized: Please log in with a valid token"));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResponse<object>.Error(403, "Access Denied"));
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<LegacyContract>();
builder.Services.AddScoped<StompWebSocketHandler>();

var app = builder.Build();

await app.Services.GetRequiredService<SchemaMigrationRunner>().MigrateAsync();
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<AdminSeedService>().SeedAsync();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors();
app.UseWebSockets();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger-ui";
    options.DocumentTitle = "Flowzy API";
    options.SwaggerEndpoint("/v3/api-docs", "Flowzy API");
});
app.MapGet("/swagger-ui.html", () => Results.Redirect("/swagger-ui/index.html")).AllowAnonymous();
app.MapGet("/v3/api-docs", (LegacyContract contract) => Results.Bytes(contract.OpenApiJson.ToArray(), "application/json")).AllowAnonymous();
app.MapGet("/swagger-ui/{file}", (string file) =>
{
    var assets = new Dictionary<string, (string Resource, string ContentType)>(StringComparer.OrdinalIgnoreCase)
    {
        ["swagger-ui.css"] = ("swagger-ui.css", "text/css"),
        ["swagger-ui-bundle.js"] = ("swagger-ui-bundle.js", "text/javascript"),
        ["swagger-ui-standalone-preset.js"] = ("swagger-ui-standalone-preset.js", "text/javascript"),
        ["favicon-16x16.png"] = ("favicon-16x16.png", "image/png"),
        ["favicon-32x32.png"] = ("favicon-32x32.png", "image/png"),
        ["oauth2-redirect.html"] = ("oauth2-redirect.html", "text/html")
    };
    if (!assets.TryGetValue(file, out var asset)) return Results.NotFound();
    var assembly = typeof(Swashbuckle.AspNetCore.SwaggerUI.SwaggerUIOptions).Assembly;
    var resourceName = $"Swashbuckle.AspNetCore.SwaggerUI.node_modules.swagger_ui_dist.{asset.Resource}";
    var stream = assembly.GetManifestResourceStream(resourceName);
    return stream is null ? Results.NotFound() : Results.Stream(stream, asset.ContentType);
}).AllowAnonymous();
app.MapGet("/actuator/health", () => Results.Json(new { status = "UP" })).AllowAnonymous();
app.Map("/ws", async context => await context.RequestServices.GetRequiredService<StompWebSocketHandler>().HandleAsync(context)).AllowAnonymous();
app.UseAuthentication();
app.UseMiddleware<PasswordChangeRequiredMiddleware>();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

public partial class Program;
