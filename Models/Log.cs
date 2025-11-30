using System;
using System.ComponentModel.DataAnnotations;

namespace _20241129402SoruCevapPortali.Models
{
    public class Log
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; }  // Kim yaptı?
        public string Action { get; set; }    // Ne yaptı? (Silme, Ekleme)
        public string Details { get; set; }   // Detay (Hangi ID, Hangi Başlık)
        public DateTime Date { get; set; } = DateTime.Now;
    }
}