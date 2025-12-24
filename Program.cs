using _20241129402SoruCevapPortali.Hubs;
using _20241129402SoruCevapPortali.Models;
using _20241129402SoruCevapPortali.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. SERVÝSLERÝN EKLENMESÝ (IOC Container)
// ==========================================

// Veritabaný Baðlantýsý
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity (Kullanýcý Yönetimi) Ayarlarý
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Þifre Kurallarý (SeedData'daki "Aa123456." þifresine uygun)
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;           // En az bir rakam
    options.Password.RequireLowercase = true;       // En az bir küçük harf
    options.Password.RequireUppercase = true;       // En az bir büyük harf
    options.Password.RequireNonAlphanumeric = true; // En az bir sembol (., !, ? vb.)

    options.User.RequireUniqueEmail = true;         // Her e-posta sadece bir kez kullanýlabilir
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie (Çerez) Ayarlarý
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";            // Giriþ yapmamýþ kullanýcý buraya yönlenir
    options.AccessDeniedPath = "/Auth/AccessDenied"; // Yetkisiz giriþ denemesi buraya yönlenir
    options.SlidingExpiration = true;             // Kullanýcý sitede aktifse süreyi uzat
    options.ExpireTimeSpan = TimeSpan.FromDays(7); // Beni hatýrla süresi
});

// Repository ve Diðer Servisler
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>)); // Generic Repository Pattern
builder.Services.AddSignalR(); // Anlýk bildirimler için
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ==========================================
// 2. MIDDLEWARE AYARLARI (Ýstek Hattý)
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // wwwroot klasörünü açar (css, js, img)

app.UseRouting();

app.UseAuthentication(); // Kimlik Doðrulama (Login oldum mu?)
app.UseAuthorization();  // Yetkilendirme (Admin miyim?)

// SignalR Hub Rotasý
app.MapHub<GeneralHub>("/general-hub");

// MVC Rotalarý
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ==========================================
// 3. SEED DATA (OTOMATÝK VERÝ DOLDURMA)
// ==========================================
try
{
    // Veritabaný yoksa oluþturur, varsa admin kontrolü yapar.
    await SeedData.TestVerileriniDoldur(app);
}
catch (Exception ex)
{
    // Hata oluþursa konsola deðil, uygulamanýn log mekanizmasýna yazar.
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Seed Data (Baþlangýç Verileri) yüklenirken bir hata oluþtu.");
}

app.Run();