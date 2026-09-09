using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PetCare.Infrastructure.Persistence;

namespace PetCare.IntegrationTests;

/// <summary>
/// Fábrica customizada de WebApplicationFactory que substitui o PetCareContext
/// (Oracle) por um provider EF Core InMemory isolado por instância de teste,
/// e substitui a autenticação JWT por um esquema fake que autentica automaticamente
/// todas as requisições, permitindo validar o pipeline HTTP completo (incluindo
/// [Authorize]) sem depender de um banco real nem de tokens JWT reais.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // ---------- Banco InMemory no lugar do Oracle ----------
            services.RemoveAll<DbContextOptions<PetCareContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<PetCareContext>>();

            services.AddDbContext<PetCareContext>(options =>
                options.UseInMemoryDatabase(DatabaseName));

            // ---------- Autenticação fake no lugar do JWT ----------
            services.AddAuthentication(defaultScheme: TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, options => { });
        });
    }

    /// <summary>Cria e migra (garante) o banco em memória, retornando um escopo para seed de dados.</summary>
    public PetCareContext CreateContext()
    {
        var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PetCareContext>();
        context.Database.EnsureCreated();
        return context;
    }
}