namespace FeEstoque.Api.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
}
