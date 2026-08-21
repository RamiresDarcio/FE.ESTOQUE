namespace FeEstoque.Api.DTOs;

public record ClienteRequest(string Nome, string? Cpf, string? Email, string? Telefone, string? Endereco);
public record VendaItemRequest(int ProdutoId, int Quantidade);
public record VendaRequest(int? ClienteId, decimal Desconto, string TipoDesconto, string FormaPagamento, decimal? ValorRecebido, List<VendaItemRequest> Itens);
public record VendaCreatedResponse(int Id, int? ClienteId, DateTime DataVenda, decimal Subtotal, decimal Desconto, decimal Total, string FormaPagamento, string Status, decimal Troco);
