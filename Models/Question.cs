using System;
using System.ComponentModel.DataAnnotations.Schema; // Bu gerekli

namespace _20241129402SoruCevapPortali.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsApproved { get; set; }
        public int CategoryId { get; set; }

        // İlişkiler
        public string UserId { get; set; }

        [ForeignKey("UserId")] // Bu attribute önemli
        public virtual AppUser User { get; set; } // <-- EKSİK OLAN BU

        public int LikeCount { get; set; } = 0;
    }
}