using System.Text.Json.Serialization;
using Api.Data;
using Api.Features;
using Api.Features.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BudgetDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("BudgetDb")));

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<BudgetDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    // LAN-only, plain-HTTP deployment: cookies must still be sent without TLS.
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

builder.Services.Configure<List<Identity.SeedUser>>(builder.Configuration.GetSection("HouseholdUsers"));

builder.AddExceptionMapper(config =>
{
    config.Map<DbUpdateException>().ToStatusCode(StatusCodes.Status409Conflict);
    config.Map<DbUpdateConcurrencyException>().ToStatusCode(StatusCodes.Status409Conflict);
});

builder.Services.AddFeatures();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<BudgetDbContext>().Database.MigrateAsync();
}

app.UseExceptionMapper();

// Plain-HTTP LAN deployment: no HTTPS redirection/HSTS.
app.UseAuthentication();
app.UseAuthorization();

app.MapFeatures();

await app.SeedFeaturesAsync();

app.Run();

public partial class Program { }
