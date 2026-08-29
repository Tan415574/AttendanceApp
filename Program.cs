using AttendanceApp.Data;
using AttendanceApp.Hubs;
using AttendanceApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=attendance.db"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Reasonable defaults for a student-facing app; tighten if your dept requires it.
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});

builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddScoped<QrCodeService>();
builder.Services.AddScoped<AttendanceImportService>();

var app = builder.Build();

// Auto-migrate + seed the two roles on startup so a fresh clone "just works".
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Lecturer", "Student" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<AttendanceHub>("/hubs/attendance");

app.Run();
