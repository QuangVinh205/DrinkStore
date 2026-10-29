using DrinkStore.Data;
using Microsoft.EntityFrameworkCore;
using DrinkStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<DrinkStoreDb>(
    opt => opt.UseSqlServer(
        builder.Configuration.GetConnectionString("DrinkStoreConnection")));


builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<DrinkStoreDb>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});


var app = builder.Build();

var demoEnabled = builder.Configuration.GetValue<bool>("Demo:Enabled");
if (demoEnabled)
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Demo mode is available only in Development.");

    // Validate the target before applying migrations or writing any demo data.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DrinkStoreDb>();
    if (!db.Database.GetDbConnection().Database.Equals("DrinkStoreDemo", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Demo mode requires a separate database named DrinkStoreDemo.");
    if (string.IsNullOrWhiteSpace(builder.Configuration["Demo:ManagerPassword"]))
        throw new InvalidOperationException("Set Demo__ManagerPassword before starting demo mode.");

    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

try
{
    await IdentitySeeder.SeedRolesAsync(app.Services);
    if (demoEnabled)
        await DemoSeeder.SeedAsync(app.Services, builder.Configuration);
}
catch (SqlException ex) when (app.Environment.IsDevelopment() && !demoEnabled)
{
    // Public pages can render locally while SQL Server is unavailable.
    // Authentication and data-backed pages still require the configured database.
    app.Logger.LogError(ex,
        "Cannot seed Identity roles. Check DrinkStoreConnection and start SQL Server, then restart the app.");
}
app.Run();
