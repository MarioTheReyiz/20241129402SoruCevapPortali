using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace _20241129402SoruCevapPortali.Models
{
    public class Answer
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public int QuestionId { get; set; }
        public DateTime Date { get; set; }
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }

        public int LikeCount { get; set; } = 0;

        public string? ImageUrl { get; set; }

        public string? VideoUrl { get; set; }
    }
}