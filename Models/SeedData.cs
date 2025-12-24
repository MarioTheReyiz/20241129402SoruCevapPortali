using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace _20241129402SoruCevapPortali.Models
{
    public static class SeedData
    {
        public static async Task TestVerileriniDoldur(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Veritabanı yoksa oluştur
                context.Database.Migrate();

                // 1. Rolleri Kontrol Et ve Oluştur
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Admin"));
                }
                if (!await roleManager.RoleExistsAsync("User"))
                {
                    await roleManager.CreateAsync(new IdentityRole("User"));
                }

                // 2. KULLANICI KONTROLÜNÜ BURADA DEĞİŞTİRDİK
                // Veritabanında hiç kullanıcı yok mu diye bakmak yerine, "admin" isimli kullanıcı var mı diye bakıyoruz.
                var adminUser = await userManager.FindByNameAsync("admin");

                if (adminUser == null)
                {
                    var newAdmin = new AppUser
                    {
                        UserName = "admin",
                        Email = "admin@portal.com",
                        Name = "Sistem",
                        Surname = "Yöneticisi",
                        PhotoUrl = "/img/adminimage.jpg",
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(newAdmin, "Aa123456.");

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newAdmin, "Admin");
                    }
                    else
                    {
                        // Hata varsa konsola yazdıralım ki neden oluşmadığını görelim (Şifre yetersizliği vb.)
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"Admin oluşturma hatası: {error.Description}");
                        }
                    }
                }

                // 3. Kategorileri Kontrol Et
                if (!context.Categories.Any())
                {
                    context.Categories.Add(new Category
                    {
                        Name = "Genel",
                        Description = "Genel Sorular"
                    });

                    context.SaveChanges();
                }
            }
        }
    }
}