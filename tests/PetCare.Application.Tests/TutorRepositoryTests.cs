using PetCare.Domain.Entities;
using PetCare.Infrastructure.Repositories;
using Xunit;

namespace PetCare.Application.Tests;

public class TutorRepositoryTests
{
    [Fact]
    public async Task ExistsByEmailAsync_EmailJaCadastrado_RetornaTrue()
    {
        // Arrange
        using var context = InMemoryContextFactory.Create();
        var repository = new TutorRepository(context);
        await repository.AddAsync(new Tutor("Maria Silva", "maria@email.com", null));

        // Act
        var resultado = await repository.ExistsByEmailAsync("maria@email.com");

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public async Task ExistsByEmailAsync_IgnorandoProprioId_RetornaFalse()
    {
        // Arrange
        using var context = InMemoryContextFactory.Create();
        var repository = new TutorRepository(context);
        var tutor = await repository.AddAsync(new Tutor("Maria Silva", "maria@email.com", null));

        // Act
        var resultado = await repository.ExistsByEmailAsync("maria@email.com", ignoreId: tutor.Id);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task GetByEmailAsync_EmailInexistente_RetornaNulo()
    {
        // Arrange
        using var context = InMemoryContextFactory.Create();
        var repository = new TutorRepository(context);

        // Act
        var resultado = await repository.GetByEmailAsync("naoexiste@email.com");

        // Assert
        Assert.Null(resultado);
    }
}
