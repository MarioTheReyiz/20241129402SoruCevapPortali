using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using _20241129402SoruCevapPortali.Hubs; 
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;    
    options.Password.RequireDigit = true;     
    options.Password.RequireLowercase = true;   
    options.Password.RequireUppercase = true;     
    options.Password.RequireNonAlphanumeric = true; 

    options.User.RequireUniqueEmail = true; 
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";    
    options.AccessDeniedPath = "/Auth/AccessDenied"; 
    options.SlidingExpiration = true;    
    options.ExpireTimeSpan = TimeSpan.FromDays(7); 
});
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<GeneralHub>("/general-hub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
try
{
    await SeedData.TestVerileriniDoldur(app);
}
catch (Exception ex)
{
    Console.WriteLine("Seed Data hatasý: " + ex.Message);
}

app.Run();