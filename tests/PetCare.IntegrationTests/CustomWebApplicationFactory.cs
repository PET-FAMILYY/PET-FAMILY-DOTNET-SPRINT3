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
/// permitindo validar o pipeline HTTP completo sem depender de um banco real.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove TODOS os descritores de configuracao do PetCareContext (Oracle),
            // nao apenas o DbContextOptions<T>: a partir do EF Core 8+/9+ o AddDbContext
            // tambem registra IDbContextOptionsConfiguration<T>, que compoe configuracoes
            // em vez de sobrescrever. Sem remover isso, Oracle + InMemory ficam registrados
            // juntos e o EF Core lanca "Only a single database provider can be registered".
            services.RemoveAll<DbContextOptions<PetCareContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<PetCareContext>>();

            services.AddDbContext<PetCareContext>(options =>
                options.UseInMemoryDatabase(DatabaseName));
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
