namespace FeEstoque.Api.DTOs;

public record LivroRequest(string Titulo, string Autor, string Editora, string Genero, int AnoPublicacao, decimal Preco, int Quantidade, string Resumo, string? Imagem);
public record EstoqueRequest(int Quantidade);
public record LoginRequest(string Login, string Senha);
