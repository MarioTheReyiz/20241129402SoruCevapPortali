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
                context.Database.Migrate();
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Admin"));
                }
                if (!await roleManager.RoleExistsAsync("User"))
                {
                    await roleManager.CreateAsync(new IdentityRole("User"));
                }
                if (!context.Users.Any())
                {
                    var adminUser = new AppUser
                    {
                        UserName = "admin",
                        Email = "admin@portal.com",
                        Name = "Sistem",
                        Surname = "Yöneticisi",
                        PhotoUrl = "/img/adminimage.jpg",
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(adminUser, "123");

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }
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