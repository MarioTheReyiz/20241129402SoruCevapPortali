using System;
using System.ComponentModel.DataAnnotations;

namespace _20241129402SoruCevapPortali.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;

        // Kime gidecek?
        public string TargetRole { get; set; } // "All", "Admin", "Moderator", "User"
        public int? TargetUserId { get; set; } // Kişiye özelse ID'si, değilse boş

        public string SenderName { get; set; } // Gönderen Admin
    }
}