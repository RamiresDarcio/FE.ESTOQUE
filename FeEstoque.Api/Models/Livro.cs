using System.ComponentModel.DataAnnotations;

namespace FeEstoque.Api.Models;

public class Livro
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string Autor { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string Editora { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Genero { get; set; } = string.Empty;

    [Range(1, 9999)]
    public int AnoPublicacao { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal Preco { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantidade { get; set; }

    [Required, MaxLength(4000)]
    public string Resumo { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Imagem { get; set; }
}
