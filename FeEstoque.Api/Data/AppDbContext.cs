using FeEstoque.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FeEstoque.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Livro> Livros => Set<Livro>();
    public DbSet<AppUser> Users => Set<AppUser>();
}
