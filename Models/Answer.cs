using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _20241129402SoruCevapPortali.Models
{
    public class Answer
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Cevap içeriği boş olamaz.")]
        public string Content { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        // Soru İlişkisi
        public int QuestionId { get; set; }
        public virtual Question Question { get; set; }

        // --- DÜZELTME BURADA ---
        // Burada da User ID tipi string olmalı.
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }
    }
}