using Microsoft.AspNetCore.Identity;

namespace _20241129402SoruCevapPortali.Models
{
    // IdentityUser sınıfı Id, UserName, PasswordHash, Email gibi alanları zaten içerir.
    // Ekstra alanlarımızı buraya ekliyoruz.
    public class AppUser : IdentityUser
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string PhotoUrl { get; set; } = "/img/undraw_profile.svg";

        // İlişkiler
        public List<Question>? Questions { get; set; }
        public List<Answer>? Answers { get; set; }
    }
}