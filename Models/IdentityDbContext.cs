using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace _20241129402SoruCevapPortali.Models
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<TicketMessage> TicketMessages { get; set; }
        public DbSet<UserReport> UserReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. KATEGORİ AYARI (ÖNEMLİ):
            // Kategori silindiğinde, o kategoriye ait tüm Sorular da otomatik silinsin.
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Category)
                .WithMany(c => c.Questions)
                .HasForeignKey(q => q.CategoryId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade: Zincirleme Silme

            // 2. KULLANICI AYARLARI:
            // Kullanıcı silinirken SQL Server'da "Cycle/Döngü" hatası almamak için 
            // burayı 'Restrict' (Kısıtla) olarak bırakıyoruz.
            // NOT: Kullanıcı silme işlemini AdminController içerisindeki DeleteUser metoduyla
            // manuel olarak temizleyerek yapmalısın.
            
            modelBuilder.Entity<Answer>()
                .HasOne(a => a.User)
                .WithMany(u => u.Answers)
                .HasForeignKey(a => a.UserId) // Modelinde AppUserId ise burayı ona göre düzelt
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.User)
                .WithMany(u => u.Questions)
                .HasForeignKey(q => q.UserId) // Modelinde AppUserId ise burayı ona göre düzelt
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}