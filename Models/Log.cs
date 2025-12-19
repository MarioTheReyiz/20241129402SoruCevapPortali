using System;
using System.ComponentModel.DataAnnotations;

namespace _20241129402SoruCevapPortali.Models
{
    public class Log
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } 
        public string Action { get; set; } 
        public string Details { get; set; } 
        public DateTime Date { get; set; } = DateTime.Now;
    }
}