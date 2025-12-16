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

        public string TargetRole { get; set; }

        // DÜZELTME: int? yerine string? yapıyoruz
        public string? TargetUserId { get; set; }

        public string SenderName { get; set; }
    }
}