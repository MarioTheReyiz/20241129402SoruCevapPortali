using System.ComponentModel.DataAnnotations;

namespace _20241129402SoruCevapPortali.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string Password { get; set; }

        public string Role { get; set; } = "User"; // Varsayılan rol: User

        // YENİ EKLENEN ALANLAR
        public string? Name { get; set; }          // Ad
        public string? Surname { get; set; }       // Soyad
        public string? Email { get; set; }         // E-Posta
        public string? PhoneNumber { get; set; }   // Telefon
        public string PhotoUrl { get; set; } = "/img/undraw_profile.svg"; // Varsayılan Resim
        public string? ResetCode { get; set; } // Şifre sıfırlama kodu için

        // İLİŞKİLER (Boş olabilir - Nullable)
        public List<Question>? Questions { get; set; }
        public List<Answer>? Answers { get; set; }

    }
}