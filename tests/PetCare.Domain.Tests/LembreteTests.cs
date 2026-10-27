using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using Xunit;

namespace PetCare.Domain.Tests;

public class LembreteTests
{
    [Fact]
    public void Construtor_DadosValidos_CriaLembreteComSucesso()
    {
        // Arrange
        var titulo = "Vacina antirrábica";
        var descricao = "Aplicar dose de reforço";
        var dataLembrete = DateTime.UtcNow.AddDays(3);
        var petId = 1;

        // Act
        var lembrete = new Lembrete(titulo, descricao, dataLembrete, petId);

        // Assert
        Assert.Equal(titulo, lembrete.Titulo);
        Assert.Equal(descricao, lembrete.Descricao);
        Assert.Equal(petId, lembrete.PetId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Construtor_TituloInvalido_LancaDomainException(string? titulo)
    {
        var exception = Record.Exception(() => new Lembrete(titulo!, "desc", DateTime.UtcNow.AddDays(1), 1));
        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void Construtor_DataLembreteAnteriorAtual_LancaDomainException()
    {
        // Arrange
        var dataPassada = DateTime.UtcNow.AddDays(-1);

        // Act
        var exception = Record.Exception(() => new Lembrete("Vacina", "desc", dataPassada, 1));

        // Assert
        Assert.IsType<DomainException>(exception);
    }
}
