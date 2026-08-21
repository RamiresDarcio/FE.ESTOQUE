namespace FeEstoque.Api.Models;

public class Venda
{
    public int Id { get; set; }
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public DateTime DataVenda { get; set; } = DateTime.UtcNow;
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public string FormaPagamento { get; set; } = string.Empty;
    public string Status { get; set; } = "Pendente";
    public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();
}
