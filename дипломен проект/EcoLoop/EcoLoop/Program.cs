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
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SeedEventsAsync(db);
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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

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
            City = "София",
            Type = "Пазар",
            ShortDescription = "Пазар с локални производители и идеи за живот без отпадък.",
            ImageUrl = "https://images.unsplash.com/photo-1488459716781-31db52582fe9?auto=format&fit=crop&w=1200&q=60"
        },
        new Event
        {
            Title = "Фестивал на рециклирането",
            Date = now.AddDays(14),
            City = "Пловдив",
            Type = "Фестивал",
            ShortDescription = "Ден с демонстрации, игри и работилници за разделно събиране.",
            ImageUrl = "https://images.unsplash.com/photo-1604187351574-c75ca79f5807?auto=format&fit=crop&w=1200&q=60"
        },
        new Event
        {
            Title = "Swap Party",
            Date = now.AddDays(10),
            City = "Варна",
            Type = "Обмен",
            ShortDescription = "Размяна на дрехи и аксесоари вместо нови покупки.",
            ImageUrl = "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?auto=format&fit=crop&w=1200&q=60"
        },
        new Event
        {
            Title = "Еко работилници",
            Date = now.AddDays(21),
            City = "Бургас",
            Type = "Обучение",
            ShortDescription = "Практични сесии за компостиране, upcycling и устойчив дом.",
            ImageUrl = "https://images.unsplash.com/photo-1461532257246-777de18cd58b?auto=format&fit=crop&w=1200&q=60"
        });

    await db.SaveChangesAsync();
}
