using System;
using System.Linq;
using System.Threading.Tasks;
using PetCare.Domain.Entities;
using PetCare.Infrastructure.Repositories;
using Xunit;

namespace PetCare.Application.Tests;

public class LembreteRepositoryTests
{
    [Fact]
    public async Task AddAsync_LembreteValido_PersisteEGeraId()
    {
        // Arrange
        using var context = InMemoryContextFactory.Create();
        var repository = new LembreteRepository(context);
        var lembrete = new Lembrete("Vacina", "Reforço anual", DateTime.UtcNow.AddDays(2), 1);

        // Act
        var resultado = await repository.AddAsync(lembrete);

        // Assert
        Assert.NotEqual(0, resultado.Id);
        Assert.Single(context.Lembretes);
    }

    [Fact]
    public async Task GetByPetIdAsync_LembretesCadastrados_RetornaSomenteLembretesDoPet()
    {
        // Arrange
        using var context = InMemoryContextFactory.Create();
        var repository = new LembreteRepository(context);
        await repository.AddAsync(new Lembrete("Vacina", null, DateTime.UtcNow.AddDays(2), 1));
        await repository.AddAsync(new Lembrete("Vermífugo", null, DateTime.UtcNow.AddDays(5), 1));
        await repository.AddAsync(new Lembrete("Banho", null, DateTime.UtcNow.AddDays(3), 2));

        // Act
        var resultado = (await repository.GetByPetIdAsync(1)).ToList();

        // Assert
        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, l => Assert.Equal(1, l.PetId));
    }
}