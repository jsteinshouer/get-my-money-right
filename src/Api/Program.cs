using System.Text.Json.Serialization;
using Api;
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

// Registered ahead of AddExceptionMapper so it intercepts DbUpdateException/DbUpdateConcurrencyException
// before ForEvolve's own pipeline gets a chance (see DbUpdateExceptionHandler for why).
builder.Services.AddExceptionHandler<DbUpdateExceptionHandler>();

builder.AddExceptionMapper();

builder.Services.AddFeatures();

builder.Services.AddDemoDataSeeder();

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

// Demo mode owns the whole database — it wipes what's there and lays down the household users
// itself — so it runs first and leaves the feature seeding below with nothing left to create.
await app.SeedDemoDataAsync();
await app.SeedFeaturesAsync();

app.Run();

public partial class Program { }
