using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using Xunit;

namespace PetCare.Domain.Tests;

public class TutorTests
{
    [Fact]
    public void Construtor_DadosValidos_CriaTutorComSucesso()
    {
        // Arrange
        var nome = "Maria Silva";
        var email = "maria@email.com";
        var telefone = "11999999999";

        // Act
        var tutor = new Tutor(nome, email, telefone);

        // Assert
        Assert.Equal(nome, tutor.Nome);
        Assert.Equal(email, tutor.Email);
        Assert.Equal(telefone, tutor.Telefone);
    }

    [Theory]
    [InlineData("", "maria@email.com")]
    [InlineData(" ", "maria@email.com")]
    [InlineData(null, "maria@email.com")]
    public void Construtor_NomeInvalido_LancaDomainException(string? nome, string email)
    {
        // Arrange & Act
        var exception = Record.Exception(() => new Tutor(nome!, email, null));

        // Assert
        Assert.IsType<DomainException>(exception);
    }

    [Theory]
    [InlineData("mariaemail.com")]
    [InlineData("maria@")]
    [InlineData("")]
    [InlineData(" ")]
    public void Construtor_EmailInvalido_LancaDomainException(string email)
    {
        // Arrange & Act
        var exception = Record.Exception(() => new Tutor("Maria Silva", email, null));

        // Assert
        Assert.IsType<DomainException>(exception);
    }

    [Fact]
    public void Atualizar_DadosValidos_AtualizaCamposComSucesso()
    {
        // Arrange
        var tutor = new Tutor("Maria Silva", "maria@email.com", "11999999999");

        // Act
        tutor.Atualizar("Maria Souza", "maria.souza@email.com", "11988888888");

        // Assert
        Assert.Equal("Maria Souza", tutor.Nome);
        Assert.Equal("maria.souza@email.com", tutor.Email);
        Assert.Equal("11988888888", tutor.Telefone);
    }
}
