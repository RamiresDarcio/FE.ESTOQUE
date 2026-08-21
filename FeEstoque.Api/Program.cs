using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FeEstoque.Api.Data;
using FeEstoque.Api.DTOs;
using FeEstoque.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var jwt = builder.Configuration.GetSection("Jwt");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .SetIsOriginAllowed(origin => origin == "null" || (Uri.TryCreate(origin, UriKind.Absolute, out var uri) && (uri.Host == "localhost" || uri.Host == "127.0.0.1")))
    .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
        ValidateIssuer = true,
        ValidIssuer = jwt["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwt["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    var hasher = new PasswordHasher<AppUser>();
    if (!db.Users.Any())
    {
        var user = new AppUser { Login = "admin" };
        user.SenhaHash = hasher.HashPassword(user, "admin123");
        db.Users.Add(user);
    }
    if (!db.Users.Any(user => user.Login == "gerente"))
    {
        var profile = new AppUser { Login = "gerente" };
        profile.SenhaHash = hasher.HashPassword(profile, "gerente123");
        db.Users.Add(profile);
    }
    db.SaveChanges();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { nome = "FE.ESTOQUE API", status = "online" }));
app.MapPost("/api/auth/login", async (LoginRequest request, AppDbContext db, IConfiguration config) =>
{
    var user = await db.Users.SingleOrDefaultAsync(item => item.Login == request.Login.Trim());
    if (user is null || new PasswordHasher<AppUser>().VerifyHashedPassword(user, user.SenhaHash, request.Senha) == PasswordVerificationResult.Failed)
        return Results.Unauthorized();

    var claims = new[] { new Claim(ClaimTypes.Name, user.Login) };
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims, expires: DateTime.UtcNow.AddHours(config.GetValue("Jwt:ExpiresInHours", 8)), signingCredentials: credentials);
    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), usuario = user.Login });
});

var livros = app.MapGroup("/api/livros").RequireAuthorization();
livros.MapGet("", async (string? busca, AppDbContext db) =>
{
    var query = db.Livros.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(busca))
    {
        var termo = busca.Trim().ToLower();
        query = query.Where(book => book.Titulo.ToLower().Contains(termo) || book.Autor.ToLower().Contains(termo) || book.Editora.ToLower().Contains(termo) || book.Genero.ToLower().Contains(termo));
    }
    return Results.Ok(await query.OrderBy(book => book.Titulo).ToListAsync());
});
livros.MapGet("/{id:int}", async (int id, AppDbContext db) => await db.Livros.FindAsync(id) is { } book ? Results.Ok(book) : Results.NotFound(new { mensagem = "Livro não encontrado." }));
livros.MapPost("", async (LivroRequest request, AppDbContext db) =>
{
    var error = Validate(request); if (error is not null) return Results.BadRequest(new { mensagem = error });
    var book = ToLivro(request); db.Livros.Add(book); await db.SaveChangesAsync(); return Results.Created($"/api/livros/{book.Id}", book);
});
livros.MapPut("/{id:int}", async (int id, LivroRequest request, AppDbContext db) =>
{
    var error = Validate(request); if (error is not null) return Results.BadRequest(new { mensagem = error });
    var book = await db.Livros.FindAsync(id); if (book is null) return Results.NotFound(new { mensagem = "Livro não encontrado." });
    book.Titulo = request.Titulo.Trim(); book.Autor = request.Autor.Trim(); book.Editora = request.Editora.Trim(); book.Genero = request.Genero.Trim(); book.AnoPublicacao = request.AnoPublicacao; book.Preco = request.Preco; book.Quantidade = request.Quantidade; book.Resumo = request.Resumo.Trim(); book.Imagem = request.Imagem;
    await db.SaveChangesAsync(); return Results.Ok(book);
});
livros.MapDelete("/{id:int}", async (int id, AppDbContext db) => { var book = await db.Livros.FindAsync(id); if (book is null) return Results.NotFound(new { mensagem = "Livro não encontrado." }); db.Livros.Remove(book); await db.SaveChangesAsync(); return Results.NoContent(); });
livros.MapPatch("/{id:int}/estoque", async (int id, EstoqueRequest request, AppDbContext db) => { if (request.Quantidade < 0) return Results.BadRequest(new { mensagem = "A quantidade não pode ser negativa." }); var book = await db.Livros.FindAsync(id); if (book is null) return Results.NotFound(new { mensagem = "Livro não encontrado." }); book.Quantidade = request.Quantidade; await db.SaveChangesAsync(); return Results.Ok(book); });

var clientes = app.MapGroup("/api/clientes").RequireAuthorization();
clientes.MapGet("", async (string? busca, AppDbContext db) =>
{
    var query = db.Clientes.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(busca)) query = query.Where(item => item.Nome.ToLower().Contains(busca.Trim().ToLower()) || (item.Cpf != null && item.Cpf.Contains(busca.Trim())));
    return Results.Ok(await query.OrderBy(item => item.Nome).ToListAsync());
});
clientes.MapGet("/{id:int}", async (int id, AppDbContext db) => await db.Clientes.FindAsync(id) is { } cliente ? Results.Ok(cliente) : Results.NotFound(new { mensagem = "Cliente não encontrado." }));
clientes.MapPost("", async (ClienteRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Nome)) return Results.BadRequest(new { mensagem = "O nome do cliente é obrigatório." });
    var cliente = new Cliente { Nome = request.Nome.Trim(), Cpf = request.Cpf?.Trim(), Email = request.Email?.Trim(), Telefone = request.Telefone?.Trim(), Endereco = request.Endereco?.Trim() };
    db.Clientes.Add(cliente); await db.SaveChangesAsync(); return Results.Created($"/api/clientes/{cliente.Id}", cliente);
});
clientes.MapPut("/{id:int}", async (int id, ClienteRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Nome)) return Results.BadRequest(new { mensagem = "O nome do cliente é obrigatório." });
    var cliente = await db.Clientes.FindAsync(id);
    if (cliente is null) return Results.NotFound(new { mensagem = "Cliente não encontrado." });
    cliente.Nome = request.Nome.Trim(); cliente.Cpf = request.Cpf?.Trim(); cliente.Email = request.Email?.Trim(); cliente.Telefone = request.Telefone?.Trim(); cliente.Endereco = request.Endereco?.Trim();
    await db.SaveChangesAsync(); return Results.Ok(cliente);
});
clientes.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var cliente = await db.Clientes.FindAsync(id);
    if (cliente is null) return Results.NotFound(new { mensagem = "Cliente não encontrado." });
    if (await db.Vendas.AnyAsync(item => item.ClienteId == id)) return Results.BadRequest(new { mensagem = "Não é possível excluir um cliente com vendas vinculadas." });
    db.Clientes.Remove(cliente); await db.SaveChangesAsync(); return Results.NoContent();
});

var vendas = app.MapGroup("/api/vendas").RequireAuthorization();
vendas.MapGet("", async (DateTime? dataInicial, DateTime? dataFinal, int? clienteId, string? formaPagamento, string? status, AppDbContext db) =>
{
    var query = db.Vendas.AsNoTracking().Include(item => item.Cliente).AsQueryable();
    if (dataInicial.HasValue) query = query.Where(item => item.DataVenda >= dataInicial.Value.Date);
    if (dataFinal.HasValue) query = query.Where(item => item.DataVenda < dataFinal.Value.Date.AddDays(1));
    if (clienteId.HasValue) query = query.Where(item => item.ClienteId == clienteId);
    if (!string.IsNullOrWhiteSpace(formaPagamento)) query = query.Where(item => item.FormaPagamento == formaPagamento);
    if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status);
    return Results.Ok(await query.OrderByDescending(item => item.DataVenda).Select(item => new { item.Id, item.DataVenda, cliente = item.Cliente == null ? "Não identificado" : item.Cliente.Nome, item.Subtotal, item.Desconto, item.Total, item.FormaPagamento, item.Status }).ToListAsync());
});
vendas.MapGet("/relatorios", async (DateTime? dataInicial, DateTime? dataFinal, AppDbContext db) =>
{
    var query = db.Vendas.AsNoTracking().Where(item => item.Status != "Cancelado");
    if (dataInicial.HasValue) query = query.Where(item => item.DataVenda >= dataInicial.Value.Date);
    if (dataFinal.HasValue) query = query.Where(item => item.DataVenda < dataFinal.Value.Date.AddDays(1));
    var vendasPeriodo = await query.ToListAsync();
    var vendaIds = vendasPeriodo.Select(item => item.Id).ToList();
    var quantidade = await db.ItensVenda.Where(item => vendaIds.Contains(item.VendaId)).SumAsync(item => (int?)item.Quantidade) ?? 0;
    return Results.Ok(new { quantidadeVendas = vendasPeriodo.Count, faturamento = vendasPeriodo.Sum(item => item.Total), descontos = vendasPeriodo.Sum(item => item.Desconto), ticketMedio = vendasPeriodo.Count == 0 ? 0 : vendasPeriodo.Average(item => item.Total), produtosVendidos = quantidade });
});
vendas.MapGet("/relatorios/produtos-mais-vendidos", async (AppDbContext db) => Results.Ok(await db.ItensVenda.AsNoTracking().Where(item => item.Venda.Status == "Pago").GroupBy(item => new { item.ProdutoId, item.Produto.Titulo }).Select(group => new { produtoId = group.Key.ProdutoId, produto = group.Key.Titulo, quantidade = group.Sum(item => item.Quantidade), faturamento = group.Sum(item => item.Subtotal) }).OrderByDescending(item => item.quantidade).Take(10).ToListAsync()));
vendas.MapGet("/cliente/{clienteId:int}", async (int clienteId, AppDbContext db) => Results.Ok(await db.Vendas.AsNoTracking().Where(item => item.ClienteId == clienteId).OrderByDescending(item => item.DataVenda).ToListAsync()));
vendas.MapGet("/{id:int}", async (int id, AppDbContext db) => await db.Vendas.AsNoTracking().Include(item => item.Cliente).Include(item => item.Itens).ThenInclude(item => item.Produto).SingleOrDefaultAsync(item => item.Id == id) is { } sale ? Results.Ok(sale) : Results.NotFound(new { mensagem = "Venda não encontrada." }));
vendas.MapGet("/{id:int}/itens", async (int id, AppDbContext db) => Results.Ok(await db.ItensVenda.AsNoTracking().Include(item => item.Produto).Where(item => item.VendaId == id).ToListAsync()));
vendas.MapPost("", async (VendaRequest request, AppDbContext db) =>
{
    var formas = new[] { "Dinheiro", "PIX", "Cartão de Débito", "Cartão de Crédito" };
    if (request.Itens is null || request.Itens.Count == 0) return Results.BadRequest(new { mensagem = "Adicione pelo menos um produto ao carrinho." });
    if (!formas.Contains(request.FormaPagamento)) return Results.BadRequest(new { mensagem = "Forma de pagamento inválida." });
    if (request.ClienteId.HasValue && await db.Clientes.FindAsync(request.ClienteId.Value) is null) return Results.BadRequest(new { mensagem = "Cliente não encontrado." });
    if (request.Desconto < 0 || (request.TipoDesconto == "percentual" && request.Desconto > 100)) return Results.BadRequest(new { mensagem = "Desconto inválido." });
    if (request.Itens.Any(item => item.Quantidade <= 0)) return Results.BadRequest(new { mensagem = "As quantidades devem ser maiores que zero." });

    await using var transaction = await db.Database.BeginTransactionAsync();
    try
    {
        var requestedItems = request.Itens.GroupBy(item => item.ProdutoId).Select(group => new VendaItemRequest(group.Key, group.Sum(item => item.Quantidade))).ToList();
        var products = await db.Livros.Where(item => requestedItems.Select(requested => requested.ProdutoId).Contains(item.Id)).ToDictionaryAsync(item => item.Id);
        if (products.Count != requestedItems.Count) return Results.BadRequest(new { mensagem = "Um ou mais produtos não foram encontrados." });
        var sale = new Venda { ClienteId = request.ClienteId, FormaPagamento = request.FormaPagamento, Status = request.FormaPagamento == "Dinheiro" ? "Pago" : "Pendente" };
        foreach (var requested in requestedItems)
        {
            var product = products[requested.ProdutoId];
            if (requested.Quantidade > product.Quantidade) return Results.BadRequest(new { mensagem = $"Estoque insuficiente para {product.Titulo}. Disponível: {product.Quantidade}." });
            var item = new ItemVenda { ProdutoId = product.Id, Quantidade = requested.Quantidade, PrecoUnitario = product.Preco, Subtotal = product.Preco * requested.Quantidade };
            sale.Itens.Add(item); sale.Subtotal += item.Subtotal; product.Quantidade -= requested.Quantidade;
        }
        sale.Desconto = request.TipoDesconto == "percentual" ? Math.Round(sale.Subtotal * request.Desconto / 100, 2) : request.Desconto;
        if (sale.Desconto > sale.Subtotal) return Results.BadRequest(new { mensagem = "O desconto não pode ser maior que o subtotal." });
        sale.Total = sale.Subtotal - sale.Desconto;
        if (request.FormaPagamento == "Dinheiro" && (!request.ValorRecebido.HasValue || request.ValorRecebido.Value < sale.Total)) return Results.BadRequest(new { mensagem = $"Valor recebido insuficiente. Total: {sale.Total:C}." });
        db.Vendas.Add(sale); await db.SaveChangesAsync(); await transaction.CommitAsync();
        return Results.Created($"/api/vendas/{sale.Id}", new VendaCreatedResponse(sale.Id, sale.ClienteId, sale.DataVenda, sale.Subtotal, sale.Desconto, sale.Total, sale.FormaPagamento, sale.Status, request.FormaPagamento == "Dinheiro" ? request.ValorRecebido!.Value - sale.Total : 0));
    }
    catch { await transaction.RollbackAsync(); return Results.Problem("Não foi possível concluir a venda.", statusCode: 500); }
});
vendas.MapPut("/{id:int}/cancelar", async (int id, AppDbContext db) =>
{
    await using var transaction = await db.Database.BeginTransactionAsync();
    var sale = await db.Vendas.Include(item => item.Itens).SingleOrDefaultAsync(item => item.Id == id);
    if (sale is null) return Results.NotFound(new { mensagem = "Venda não encontrada." });
    if (sale.Status == "Cancelado") return Results.BadRequest(new { mensagem = "A venda já está cancelada." });
    foreach (var item in sale.Itens) { var product = await db.Livros.FindAsync(item.ProdutoId); if (product is not null) product.Quantidade += item.Quantidade; }
    sale.Status = "Cancelado"; await db.SaveChangesAsync(); await transaction.CommitAsync(); return Results.Ok(new { sale.Id, sale.Status });
});
vendas.MapPut("/{id:int}/pagar", async (int id, AppDbContext db) =>
{
    var sale = await db.Vendas.FindAsync(id);
    if (sale is null) return Results.NotFound(new { mensagem = "Venda não encontrada." });
    if (sale.Status == "Cancelado") return Results.BadRequest(new { mensagem = "Uma venda cancelada não pode ser paga." });
    sale.Status = "Pago"; await db.SaveChangesAsync(); return Results.Ok(new { sale.Id, sale.Status });
});

app.MapGet("/api/dashboard", async (AppDbContext db) =>
{
    var today = DateTime.UtcNow.Date; var month = new DateTime(today.Year, today.Month, 1);
    var sales = db.Vendas.AsNoTracking().Where(item => item.Status == "Pago");
    var todaySales = await sales.Where(item => item.DataVenda >= today).ToListAsync(); var monthSales = await sales.Where(item => item.DataVenda >= month).ToListAsync();
    return Results.Ok(new { totalLivros = await db.Livros.CountAsync(), totalEstoque = await db.Livros.SumAsync(book => (int?)book.Quantidade) ?? 0, valorEstoque = await db.Livros.SumAsync(book => (decimal?)(book.Preco * book.Quantidade)) ?? 0, estoqueBaixo = await db.Livros.CountAsync(book => book.Quantidade <= 3), vendasHoje = todaySales.Count, vendasMes = monthSales.Count, faturamentoHoje = todaySales.Sum(item => item.Total), faturamentoMes = monthSales.Sum(item => item.Total), ticketMedio = monthSales.Count == 0 ? 0 : monthSales.Average(item => item.Total) });
}).RequireAuthorization();

app.Run();

static Livro ToLivro(LivroRequest request) => new() { Titulo = request.Titulo.Trim(), Autor = request.Autor.Trim(), Editora = request.Editora.Trim(), Genero = request.Genero.Trim(), AnoPublicacao = request.AnoPublicacao, Preco = request.Preco, Quantidade = request.Quantidade, Resumo = request.Resumo.Trim(), Imagem = request.Imagem };
static string? Validate(LivroRequest request) => string.IsNullOrWhiteSpace(request.Titulo) || string.IsNullOrWhiteSpace(request.Autor) || string.IsNullOrWhiteSpace(request.Editora) || string.IsNullOrWhiteSpace(request.Genero) || string.IsNullOrWhiteSpace(request.Resumo) ? "Preencha todos os campos obrigatórios." : request.AnoPublicacao < 1 || request.AnoPublicacao > DateTime.UtcNow.Year ? "Informe um ano de publicação válido." : request.Preco < 0 ? "O preço não pode ser negativo." : request.Quantidade < 0 ? "A quantidade não pode ser negativa." : null;
