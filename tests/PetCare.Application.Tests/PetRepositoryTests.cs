using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using PetCare.Infrastructure.Repositories;
using Xunit;

namespace PetCare.Application.Tests;

public class PetRepositoryTests
{
    [Fact]
    public async Task AddAsync_TutorExistente_PersistePetComSucesso()
    {
        // Arrange
        using var context = InMemoryContextFactory.Create();
        var tutorRepositoryMock = new Mock<IRepository<Tutor>>();
        tutorRepositoryMock.Setup(r => r.ExistsByIdAsync(1)).ReturnsAsync(true);

        var repository = new PetRepository(context, tutorRepositoryMock.Object);
        var pet = new Pet("Rex", "Cachorro", "Labrador", DateTime.UtcNow.AddYears(-1), 1);

        // Act
        var resultado = await repository.AddAsync(pet);

        // Assert
        Assert.NotEqual(0, resultado.Id);
        tutorRepositoryMock.Verify(r => r.ExistsByIdAsync(1), Times.Once);
        Assert.Single(context.Pets);
    }

    [Fact]
    public async Task AddAsync_TutorInexistente_LancaResourceNotFoundExceptionENaoPersiste()
    {
        // Arrange
        using var context = InMemoryContextFactory.Create();
        var tutorRepositoryMock = new Mock<IRepository<Tutor>>();
        tutorRepositoryMock.Setup(r => r.ExistsByIdAsync(999)).ReturnsAsync(false);

        var repository = new PetRepository(context, tutorRepositoryMock.Object);
        var pet = new Pet("Rex", "Cachorro", "Labrador", DateTime.UtcNow.AddYears(-1), 999);

        // Act
        var exception = await Record.ExceptionAsync(() => repository.AddAsync(pet));

        // Assert
        Assert.IsType<ResourceNotFoundException>(exception);
        Assert.Empty(context.Pets);
    }

    [Fact]
    public async Task GetByTutorIdAsync_PetsCadastrados_RetornaSomentePetsDoTutor()
    {
        // Arrange
        using var context = InMemoryContextFactory.Create();
        var tutorRepositoryMock = new Mock<IRepository<Tutor>>();
        tutorRepositoryMock.Setup(r => r.ExistsByIdAsync(It.IsAny<int>())).ReturnsAsync(true);

        var repository = new PetRepository(context, tutorRepositoryMock.Object);
        await repository.AddAsync(new Pet("Rex", "Cachorro", null, DateTime.UtcNow.AddYears(-1), 1));
        await repository.AddAsync(new Pet("Miau", "Gato", null, DateTime.UtcNow.AddYears(-2), 2));

        // Act
        var resultado = await repository.GetByTutorIdAsync(1);

        // Assert
        Assert.Single(resultado);
        Assert.Equal("Rex", resultado.First().Nome);
    }
}
