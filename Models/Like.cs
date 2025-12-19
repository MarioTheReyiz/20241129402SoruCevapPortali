using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _20241129402SoruCevapPortali.Models
{
    public class Like
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }

        public int? QuestionId { get; set; }
        public int? AnswerId { get; set; } 

        public DateTime Date { get; set; } = DateTime.Now;
    }
}