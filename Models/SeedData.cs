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
        // Metodu async yaptık çünkü Identity işlemleri asenkrondur
        public static async Task TestVerileriniDoldur(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                // 1. Context ve Manager'ları doğru tiplerle çağırıyoruz
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Migration kontrolü
                context.Database.Migrate();

                // 2. Rolleri Oluştur (Eğer yoksa)
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Admin"));
                }
                if (!await roleManager.RoleExistsAsync("User"))
                {
                    await roleManager.CreateAsync(new IdentityRole("User"));
                }

                // 3. Admin Kullanıcısını Oluştur (UserManager kullanarak)
                if (!context.Users.Any())
                {
                    var adminUser = new AppUser
                    {
                        UserName = "admin",
                        Email = "admin@portal.com", // Identity için email gerekebilir
                        Name = "Sistem",
                        Surname = "Yöneticisi",
                        PhotoUrl = "/img/adminimage.jpg",
                        EmailConfirmed = true
                    };

                    // "123" şifresini otomatik hashleyerek kullanıcıyı oluşturur
                    var result = await userManager.CreateAsync(adminUser, "123");

                    if (result.Succeeded)
                    {
                        // Kullanıcıya Admin rolünü ata
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }

                // 4. Kategorileri Ekle
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