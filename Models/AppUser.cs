using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace _20241129402SoruCevapPortali.Models
{
    public class AppUser : IdentityUser
    {
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? PhotoUrl { get; set; } = "/img/undraw_profile.svg";

        // İlişkiler
        public virtual List<Question>? Questions { get; set; }
        public virtual List<Answer>? Answers { get; set; }
        public int ExperiencePoints { get; set; } = 0; 
        public int Level { get; set; } = 1;  
        public string Badge { get; set; } = "Noob";   
    }
}