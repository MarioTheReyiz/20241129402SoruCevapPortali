using System.ComponentModel.DataAnnotations;

namespace _20241129402SoruCevapPortali.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string? Password { get; set; }

        public string Role { get; set; } = "User";
        public string? Name { get; set; }          
        public string? Surname { get; set; }    
        public string? Email { get; set; }     
        public string? PhoneNumber { get; set; }  
        public string PhotoUrl { get; set; } = "/img/undraw_profile.svg";
        public string? ResetCode { get; set; } 
        public List<Question>? Questions { get; set; }
        public List<Answer>? Answers { get; set; }

    }
}