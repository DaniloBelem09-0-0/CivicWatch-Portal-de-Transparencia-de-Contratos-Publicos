using System.ComponentModel.DataAnnotations;
using System;

namespace CivicWatch.Api.Models
{
    public class FonteDadosPublica
    {
        [Key]
        public int Id { get; set; }
        
        [Required, StringLength(100)]
        public required string Nome { get; set; }

        [Required]
        public required string UrlBase { get; set; }
        
        public DateTime? UltimaSincronizacao { get; set; }

        [StringLength(50)]
        public string? Status { get; set; } 
    }
}