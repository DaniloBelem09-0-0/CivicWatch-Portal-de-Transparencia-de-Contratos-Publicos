using System.ComponentModel.DataAnnotations;

namespace CivicWatch.Api.DTOs
{
    public class FornecedorCadastroDto
    {
        [Required]
        [StringLength(18)]
        public string Documento { get; set; } = string.Empty; // CNPJ ou CPF

        [Required]
        [StringLength(255)]
        public string RazaoSocial { get; set; } = string.Empty;

        public string? Email { get; set; }
    }
}