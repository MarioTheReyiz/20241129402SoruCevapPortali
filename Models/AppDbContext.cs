using Microsoft.EntityFrameworkCore;

namespace _20241129402SoruCevapPortali.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }

        // --- BU KISMI EKLE ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cevap tablosunda User ilişkisi için Cascade silmeyi kapatıyoruz (Restrict yapıyoruz)
            modelBuilder.Entity<Answer>()
                .HasOne(a => a.User)
                .WithMany() // User modelinde ICollection<Answer> varsa parantez içine yazabilirsin, yoksa boş kalabilir
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict); // "No Action" mantığı

            // Aynısını Question için de garantiye almak adına yapabiliriz ama genelde biri yeterlidir.
            // Yukarıdaki kod sorunu çözer.

            base.OnModelCreating(modelBuilder);
        }
    }
}