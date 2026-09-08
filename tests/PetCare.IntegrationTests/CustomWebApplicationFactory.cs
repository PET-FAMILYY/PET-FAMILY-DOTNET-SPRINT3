using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PetCareContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

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
