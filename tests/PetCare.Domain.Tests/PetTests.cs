using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using Xunit;

namespace PetCare.Domain.Tests;

public class PetTests
{
    [Fact]
    public void Construtor_DadosValidos_CriaPetComSucesso()
    {
        // Arrange
        var nome = "Rex";
        var especie = "Cachorro";
        var raca = "Labrador";
        var dataNascimento = DateTime.UtcNow.AddYears(-2);
        var tutorId = 1;

        // Act
        var pet = new Pet(nome, especie, raca, dataNascimento, tutorId);

        // Assert
        Assert.Equal(nome, pet.Nome);
        Assert.Equal(especie, pet.Especie);
        Assert.Equal(raca, pet.Raca);
        Assert.Equal(tutorId, pet.TutorId);
    }

    [Theory]
    [InlineData("", "Cachorro")]
    [InlineData(" ", "Cachorro")]
    [InlineData(null, "Cachorro")]
    public void Construtor_NomeInvalido_LancaDomainException(string? nome, string especie)
    {
        var exception = Record.Exception(() => new Pet(nome!, especie, null, DateTime.UtcNow, 1));
        Assert.IsType<DomainException>(exception);
    }

    [Theory]
    [InlineData("Rex", "")]
    [InlineData("Rex", " ")]
    [InlineData("Rex", null)]
    public void Construtor_EspecieInvalida_LancaDomainException(string nome, string? especie)
    {
        var exception = Record.Exception(() => new Pet(nome, especie!, null, DateTime.UtcNow, 1));
        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void Construtor_DataNascimentoFutura_LancaDomainException()
    {
        // Arrange
        var dataFutura = DateTime.UtcNow.AddDays(1);

        // Act
        var exception = Record.Exception(() => new Pet("Rex", "Cachorro", null, dataFutura, 1));

        // Assert
        Assert.IsType<DomainException>(exception);
    }
}
