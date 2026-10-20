using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PC_Parts_Scrapper.Data;
using PC_Parts_Scrapper.Models;
using PC_Parts_Scrapper.Services;
using PC_Parts_Scrapper.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using DotNetEnv;

// Local dev only: traverse up the tree to find the repo root .env.
Env.TraversePath().NoClobber().Load();

var builder = WebApplication.CreateBuilder(args);

// 1. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    var databaseUri = new Uri(databaseUrl);
    var userInfo = databaseUri.UserInfo.Split(':', 2);

    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";

    var port = databaseUri.Port > 0 ? databaseUri.Port : 5432;
    var database = databaseUri.AbsolutePath.TrimStart('/');

    connectionString = $"Host={databaseUri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database configuration is missing. Set ConnectionStrings:DefaultConnection or DATABASE_URL.");
}

builder.Services.AddDbContext<PcPartsContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<PcPartsContext>()
    .AddDefaultTokenProviders();

// 3. Configure JWT Authentication
//var jwtSettings = builder.Configuration.GetSection("JwtSettings");
//var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

//using auth0
var auth0Domain = builder.Configuration["Auth0:Domain"];
var authAudience = builder.Configuration["Auth0:Audience"];

// Auth0 issuer/authority must be an https URL ending with a trailing slash.
// Accept a bare domain ("dev-xxx.eu.auth0.com") in config and normalise it here.
if (!string.IsNullOrWhiteSpace(auth0Domain))
{
    if (!auth0Domain.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        auth0Domain = $"https://{auth0Domain}";
    if (!auth0Domain.EndsWith("/"))
        auth0Domain += "/";
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.Authority = auth0Domain;
        options.Audience = authAudience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = auth0Domain,
            ValidateAudience = true,
            ValidAudience = authAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });
builder.Services.AddAuthorization();


builder.Services.AddHostedService<ScrapperWorker>();
builder.Services.AddTransient<HtmlScraperService>();
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<PredictionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PcPartsContext>();
    var scraper = scope.ServiceProvider.GetRequiredService<HtmlScraperService>();
    db.Database.Migrate();
    await scraper.CheckPriceAlertsAsync();

    //await scraper.AssignCategoriesToExistingProducts();
    //await scraper.BackfillBrandsAndGraphicsTypes();
    //return;
}



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseMiddleware<PC_Parts_Scrapper.Middleware.Auth0UserSyncMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();