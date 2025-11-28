using System.ComponentModel.DataAnnotations;
namespace _20241129402SoruCevapPortali.Models
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsApproved { get; set; } = false;

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
