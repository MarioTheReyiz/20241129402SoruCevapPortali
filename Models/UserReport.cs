using System;
using System.ComponentModel.DataAnnotations; // Key için gerekebilir

namespace _20241129402SoruCevapPortali.Models
{
    public class UserReport
    {
        [Key]
        public int Id { get; set; }

        public string ReporterId { get; set; }
        public string ReportedUserId { get; set; }

        public string Reason { get; set; }

        // DEĞİŞİKLİK BURADA: string? yaptık (Boş olabilir)
        public string? Description { get; set; }

        // DEĞİŞİKLİK BURADA: string? yaptık (Resim yüklemek zorunlu değil)
        public string? ScreenshotUrl { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;
        public bool IsReviewed { get; set; } = false;
    }
}