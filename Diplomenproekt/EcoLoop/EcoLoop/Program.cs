using EcoLoop.Data;
using EcoLoop.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
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
            Title = "Работилница за защита на пчелите 🐝",
            Date = now.AddDays(7),
            City = "София",
            Address = "ул. „Граф Игнатиев“ 12",
            Type = "Биоразнообразие",
            ShortDescription = "Експерти ще разкажат за ролята на пчелите в природата и как всеки може да помогне за тяхното опазване.",
            ImageUrl = "https://images.unsplash.com/photo-1488459716781-31db52582fe9?auto=format&fit=crop&w=1200&q=60"
        },
        new Event
        {
            Title = "Семинар „Нулев отпадък“🌱",
            Date = now.AddDays(14),
            City = "София",
            Address = "ул. „Алабин“ 33",
            Type = "Еко образование",
            ShortDescription = "Практични съвети как да намалим отпадъците в ежедневието си.",
            ImageUrl = "https://images.unsplash.com/photo-1604187351574-c75ca79f5807?auto=format&fit=crop&w=1200&q=60"
        },
        new Event
        {
            Title = "Лекция „Климатични решения за града“🌍",
            Date = now.AddDays(10),
            City = "София",
            Address = "бул. „Славянски“ 31",
            Type = "Еко образование",
            ShortDescription = "Експерти по устойчиво развитие представят практически решения за намаляване на въглеродния отпечатък в градска среда.",
            ImageUrl = "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?auto=format&fit=crop&w=1200&q=60"
        },
        new Event
        {
            Title = "Ден без автомобили",
            Date = now.AddDays(21),
            City = "Пловдив",
            Address= " бул. „Руски“ 17",
            Type = "Устойчива мобилност",
            ShortDescription = "Улица се затваря за автомобили и се превръща в пространство за велосипедисти, пешеходци и семейни активности. Целта е да се насърчи използването на устойчив транспорт.",
            ImageUrl = "https://images.unsplash.com/photo-1461532257246-777de18cd58b?auto=format&fit=crop&w=1200&q=60"
        });

    await db.SaveChangesAsync();
}
