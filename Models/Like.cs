using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _20241129402SoruCevapPortali.Models
{
    public class Like
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } // Kim beğendi?

        public int? QuestionId { get; set; } // Hangi soruyu? (Boş olabilir)
        public int? AnswerId { get; set; }   // Hangi cevabı? (Boş olabilir)

        public DateTime Date { get; set; } = DateTime.Now;
    }
}