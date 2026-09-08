using Moq;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using PetCare.Infrastructure.Repositories;
using Xunit;

namespace PetCare.Application.Tests;

public class LembreteRepositoryTests
{
    [Fact]
    public async Task AddAsync_PetExistente_PersisteLembreteComSucesso()
    {
        // Arrange
        using var context = InMemoryContextFactory.Create();
        var petRepositoryMock = new Mock<IRepository<Pet>>();
        petRepositoryMock.Setup(r => r.ExistsByIdAsync(1)).ReturnsAsync(true);

        var repository = new LembreteRepository(context, petRepositoryMock.Object);
        var lembrete = new Lembrete("Vacina", "Reforço anual", DateTime.UtcNow.AddDays(2), 1);

        // Act
        var resultado = await repository.AddAsync(lembrete);

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

        var repository = new LembreteRepository(context, petRepositoryMock.Object);
        var lembrete = new Lembrete("Vacina", "Reforço anual", DateTime.UtcNow.AddDays(2), 999);

        // Act
        var exception = await Record.ExceptionAsync(() => repository.AddAsync(lembrete));

        // Assert
        Assert.IsType<ResourceNotFoundException>(exception);
        petRepositoryMock.Verify(r => r.ExistsByIdAsync(999), Times.Once);
        Assert.Empty(context.Lembretes);
    }
}
