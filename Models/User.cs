using System.ComponentModel.DataAnnotations;

namespace _20241129402SoruCevapPortali.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [StringLength(20)]
        public string Role { get; set; }
        
        public string PhotoUrl { get; set; }

        public List<Question> Questions { get; set; }
    }
}
