using Microsoft.EntityFrameworkCore;
using PC_Parts_Scrapper.Data;
using PC_Parts_Scrapper.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5173") // Vite or Create React App URLs
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException($"Connection string 'DefaultConnection' not found.");

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    var databaseUri = new Uri(databaseUrl);
    var userInfo = databaseUri.UserInfo.Split(':');
    connectionString = $"Host={databaseUri.Host};Port={databaseUri.Port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
}

builder.Services.AddDbContextPool<PcPartsContext>(opt =>
    opt.UseNpgsql(connectionString));

builder.Services.AddHostedService<ScrapperWorker>();         //register background service of Worker 
builder.Services.AddTransient<HtmlScraperService>();        //create new instance of Scraper Service 

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseCors("AllowReactApp");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PcPartsContext>();
    db.Database.Migrate(); // Auto-creates database & tables on container startup
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();
//DOTNET 10 / 9 feature
//app.MapStaticAssets();
app.UseStaticFiles(); // Serve static files from wwwroot

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
//    .WithStaticAssets();  DOTNET 10 / 9 feature
app.MapControllers();

app.Run();
