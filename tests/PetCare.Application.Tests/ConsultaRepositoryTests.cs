using Moq;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using PetCare.Infrastructure.Repositories;
using Xunit;

namespace PetCare.Application.Tests;

public class ConsultaRepositoryTests
{
    [Fact]
    public async Task AddAsync_PetExistente_PersisteConsultaComSucesso()
    {
        // Arrange
        using var context = InMemoryContextFactory.Create();
        var petRepositoryMock = new Mock<IRepository<Pet>>();
        petRepositoryMock.Setup(r => r.ExistsByIdAsync(1)).ReturnsAsync(true);

        var repository = new ConsultaRepository(context, petRepositoryMock.Object);
        var consulta = new Consulta(DateTime.UtcNow.AddDays(1), "Consulta de rotina", 1);

        // Act
        var resultado = await repository.AddAsync(consulta);

        // Assert
        Assert.NotEqual(0, resultado.Id);
        petRepositoryMock.Verify(r => r.ExistsByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task AddAsync_PetInexistente_LancaResourceNotFoundExceptionENaoPersiste()
    {
        // Arrange
        using var context = InMemoryContextFactory.Create();
        var petRepositoryMock = new Mock<IRepository<Pet>>();
        petRepositoryMock.Setup(r => r.ExistsByIdAsync(999)).ReturnsAsync(false);

        var repository = new ConsultaRepository(context, petRepositoryMock.Object);
        var consulta = new Consulta(DateTime.UtcNow.AddDays(1), "Consulta de rotina", 999);

        // Act
        var exception = await Record.ExceptionAsync(() => repository.AddAsync(consulta));

        // Assert
        Assert.IsType<ResourceNotFoundException>(exception);
        petRepositoryMock.Verify(r => r.ExistsByIdAsync(999), Times.Once);
        Assert.Empty(context.Consultas);
    }
}
