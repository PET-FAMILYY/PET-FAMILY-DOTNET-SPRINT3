using PetCare.Domain.Entities;
using PetCare.Infrastructure.Repositories;
using Xunit;

namespace PetCare.Application.Tests;

public class ConsultaRepositoryTests
{
    [Fact]
    public async Task AddAsync_ConsultaValida_PersisteEGeraId()
    {
        // Arrange
        using var context = InMemoryContextFactory.Create();
        var repository = new ConsultaRepository(context);
        var consulta = new Consulta(DateTime.UtcNow.AddDays(1), "Consulta de rotina", 1);

        // Act
        var resultado = await repository.AddAsync(consulta);

        // Assert
        Assert.NotEqual(0, resultado.Id);
        Assert.Single(context.Consultas);
    }

    [Fact]
    public async Task GetByPetIdAsync_ConsultasCadastradas_RetornaSomenteConsultasDoPet()
    {
        // Arrange
        using var context = InMemoryContextFactory.Create();
        var repository = new ConsultaRepository(context);
        await repository.AddAsync(new Consulta(DateTime.UtcNow.AddDays(1), "Rotina", 1));
        await repository.AddAsync(new Consulta(DateTime.UtcNow.AddDays(2), "Retorno", 1));
        await repository.AddAsync(new Consulta(DateTime.UtcNow.AddDays(3), "Vacina", 2));

        // Act
        var resultado = (await repository.GetByPetIdAsync(1)).ToList();

        // Assert
        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, c => Assert.Equal(1, c.PetId));
    }
}