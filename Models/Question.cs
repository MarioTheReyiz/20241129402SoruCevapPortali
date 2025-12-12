using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _20241129402SoruCevapPortali.Models
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Başlık zorunludur.")]
        [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "İçerik zorunludur.")]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = false;

        // Kategori İlişkisi
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        // --- DÜZELTME BURADA ---
        // Identity kullandığımız için User ID tipi string (GUID) olmalı.
        public string UserId { get; set; }

        // İlişkiyi yeni oluşturduğumuz AppUser ile kuruyoruz.
        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }
    }
}