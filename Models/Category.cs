using System.ComponentModel.DataAnnotations;    

namespace _20241129402SoruCevapPortali.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı gereklidir.")]
            [StringLength(50)]
            public string Name { get; set; }

            public string Description { get; set; }
            
            public List<Question>? Questions { get; set; }
    }
}
