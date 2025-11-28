using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _20241129402SoruCevapPortali.Models
{
    public class Answer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } // Cevap metni

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Hangi soruya cevap verildi?
        public int QuestionId { get; set; }
        public Question Question { get; set; }

        // Kim cevapladı?
        public int UserId { get; set; }
        public User User { get; set; }
    }
}