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
    .SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) && (uri.Host == "localhost" || uri.Host == "127.0.0.1"))
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
    if (!db.Users.Any())
    {
        var hasher = new PasswordHasher<AppUser>();
        var user = new AppUser { Login = "admin" };
        user.SenhaHash = hasher.HashPassword(user, "admin123");
        db.Users.Add(user);
        db.SaveChanges();
    }
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

app.MapGet("/api/dashboard", async (AppDbContext db) => Results.Ok(new { totalLivros = await db.Livros.CountAsync(), totalEstoque = await db.Livros.SumAsync(book => (int?)book.Quantidade) ?? 0, valorEstoque = await db.Livros.SumAsync(book => (decimal?)(book.Preco * book.Quantidade)) ?? 0, estoqueBaixo = await db.Livros.CountAsync(book => book.Quantidade <= 3) })).RequireAuthorization();

app.Run();

static Livro ToLivro(LivroRequest request) => new() { Titulo = request.Titulo.Trim(), Autor = request.Autor.Trim(), Editora = request.Editora.Trim(), Genero = request.Genero.Trim(), AnoPublicacao = request.AnoPublicacao, Preco = request.Preco, Quantidade = request.Quantidade, Resumo = request.Resumo.Trim(), Imagem = request.Imagem };
static string? Validate(LivroRequest request) => string.IsNullOrWhiteSpace(request.Titulo) || string.IsNullOrWhiteSpace(request.Autor) || string.IsNullOrWhiteSpace(request.Editora) || string.IsNullOrWhiteSpace(request.Genero) || string.IsNullOrWhiteSpace(request.Resumo) ? "Preencha todos os campos obrigatórios." : request.AnoPublicacao < 1 || request.AnoPublicacao > DateTime.UtcNow.Year ? "Informe um ano de publicação válido." : request.Preco < 0 ? "O preço não pode ser negativo." : request.Quantidade < 0 ? "A quantidade não pode ser negativa." : null;
