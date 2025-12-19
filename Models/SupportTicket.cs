using System;
using System.Collections.Generic;

namespace _20241129402SoruCevapPortali.Models
{
    public class SupportTicket
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsClosed { get; set; } = false;

        public string UserId { get; set; }
        public AppUser User { get; set; }

        public List<TicketMessage> Messages { get; set; }
    }
}