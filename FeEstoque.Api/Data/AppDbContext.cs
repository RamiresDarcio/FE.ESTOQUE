using FeEstoque.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FeEstoque.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Livro> Livros => Set<Livro>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Venda>().Property(item => item.Subtotal).HasPrecision(18, 2);
        modelBuilder.Entity<Venda>().Property(item => item.Desconto).HasPrecision(18, 2);
        modelBuilder.Entity<Venda>().Property(item => item.Total).HasPrecision(18, 2);
        modelBuilder.Entity<ItemVenda>().Property(item => item.PrecoUnitario).HasPrecision(18, 2);
        modelBuilder.Entity<ItemVenda>().Property(item => item.Subtotal).HasPrecision(18, 2);
        modelBuilder.Entity<Venda>().HasOne(item => item.Cliente).WithMany(item => item.Vendas).HasForeignKey(item => item.ClienteId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ItemVenda>().HasOne(item => item.Produto).WithMany().HasForeignKey(item => item.ProdutoId).OnDelete(DeleteBehavior.Restrict);
    }
}
