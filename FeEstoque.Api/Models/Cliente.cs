using System.ComponentModel.DataAnnotations;

namespace FeEstoque.Api.Models;

public class Cliente
{
    public int Id { get; set; }
    [Required, MaxLength(160)] public string Nome { get; set; } = string.Empty;
    [MaxLength(14)] public string? Cpf { get; set; }
    [EmailAddress, MaxLength(200)] public string? Email { get; set; }
    [MaxLength(30)] public string? Telefone { get; set; }
    [MaxLength(400)] public string? Endereco { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
}
