using System;

namespace _20241129402SoruCevapPortali.Models
{
    public class TicketMessage
    {
        public int Id { get; set; }
        public int SupportTicketId { get; set; }
        public string SenderId { get; set; }
        public string Content { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }
}