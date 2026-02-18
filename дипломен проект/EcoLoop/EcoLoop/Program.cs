using EcoLoop.Data;
using EcoLoop.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    await SeedEventsAsync(db);
    await SeedRolesAndAdminAsync(roleManager, userManager, db);
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static async Task SeedRolesAndAdminAsync(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager, ApplicationDbContext db)
{
    string[] roles = [UserRoleType.User, UserRoleType.Producer, UserRoleType.Admin, UserRoleType.Moderator];

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    const string adminEmail = "admin@ecoloop.local";
    const string adminPassword = "Admin123!";

    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(admin, adminPassword);
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, UserRoleType.Admin);

            db.UserProfiles.Add(new UserProfile
            {
                UserId = admin.Id,
                Username = "EcoLoop Admin",
                Role = UserRoleType.Admin,
                Level = "System",
                SavedPackages = 0,
                StoresVisited = 0,
                AddedObjects = 0
            });

            await db.SaveChangesAsync();
        }
    }
}
static async Task SeedEventsAsync(ApplicationDbContext db)
{
    if (await db.Events.AnyAsync())
    {
        return;
    }

    var now = DateTime.Today;
    db.Events.AddRange(
        new Event
        {
            Title = "Zero Waste Market",
            Date = now.AddDays(7),
            City = "�����",
            Type = "�����",
            ShortDescription = "����� � ������� ������������� � ���� �� ����� ��� �������.",
            ImageUrl = "https://images.unsplash.com/photo-1488459716781-31db52582fe9?auto=format&fit=crop&w=1200&q=60"
        },
        new Event
        {
            Title = "�������� �� �������������",
            Date = now.AddDays(14),
            City = "�������",
            Type = "��������",
            ShortDescription = "��� � ������������, ���� � ����������� �� �������� ��������.",
            ImageUrl = "https://images.unsplash.com/photo-1604187351574-c75ca79f5807?auto=format&fit=crop&w=1200&q=60"
        },
        new Event
        {
            Title = "Swap Party",
            Date = now.AddDays(10),
            City = "�����",
            Type = "�����",
            ShortDescription = "������� �� ����� � ��������� ������ ���� �������.",
            ImageUrl = "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?auto=format&fit=crop&w=1200&q=60"
        },
        new Event
        {
            Title = "��� �����������",
            Date = now.AddDays(21),
            City = "������",
            Type = "��������",
            ShortDescription = "��������� ����� �� ������������, upcycling � �������� ���.",
            ImageUrl = "https://images.unsplash.com/photo-1461532257246-777de18cd58b?auto=format&fit=crop&w=1200&q=60"
        });

    await db.SaveChangesAsync();
}
