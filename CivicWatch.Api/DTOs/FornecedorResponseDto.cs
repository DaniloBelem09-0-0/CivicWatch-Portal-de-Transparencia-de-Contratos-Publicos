namespace CivicWatch.Api.DTOs
{
    public class FornecedorResponseDto
    {
        public int Id { get; set; }
        public string Documento { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string? StatusReceita { get; set; }
        public bool EmConformidade { get; set; }
    }
}