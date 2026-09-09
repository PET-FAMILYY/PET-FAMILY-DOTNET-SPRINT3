using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using PetCare.Infrastructure.Persistence;

namespace PetCare.IntegrationTests;

/// <summary>
/// Fábrica customizada de WebApplicationFactory que substitui o PetCareContext
/// (Oracle) por um provider EF Core InMemory isolado por instância de teste.
/// A autenticação JWT real é mantida, e um token válido pode ser gerado via
/// GerarTokenDeTeste() para uso nos testes que exigem [Authorize].
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PetCareContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<PetCareContext>>();

            services.AddDbContext<PetCareContext>(options =>
                options.UseInMemoryDatabase(DatabaseName));
        });
    }

    public PetCareContext CreateContext()
    {
        var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PetCareContext>();
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// Gera um token JWT válido usando a mesma chave/emissor/audiência configurados
    /// em appsettings (seção Jwt), permitindo que os testes de integração autentiquem
    /// de verdade contra o pipeline JwtBearer real, em vez de simular a autenticação.
    /// </summary>
    public string GerarTokenDeTeste(string nome = "usuario.teste", string email = "teste@petcare.com", string role = "User")
    {
        using var scope = Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var jwtSection = configuration.GetSection("Jwt");
        var chave = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key não configurada.");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "1"),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Name, nome),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Cria um HttpClient já autenticado com um token JWT de teste.</summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        var token = GerarTokenDeTeste();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}