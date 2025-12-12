using Microsoft.EntityFrameworkCore;

namespace _20241129402SoruCevapPortali.Models
{
    public static class SeedData
    {
        public static void TestVerileriniDoldur(IApplicationBuilder app)
        {
            var context = app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<IdentityDbContext>();

            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }

            if (!context.Users.Any())
            {
                context.Users.Add(new User
                {
                    Username = "admin",
                    Password = "123",
                    Role = "Admin",
                    PhotoUrl = "/img/adminimage.jpg"
                });

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
