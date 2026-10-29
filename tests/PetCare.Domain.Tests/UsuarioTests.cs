using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using Xunit;

namespace PetCare.Domain.Tests;

public class UsuarioTests
{
    [Fact]
    public void Construtor_DadosValidos_NormalizaNomeEEmailEUsaRoleUserPorPadrao()
    {
        // Arrange & Act
        var usuario = new Usuario("  Ana  ", "  ANA@Email.COM ", "hash");

        // Assert
        Assert.Equal("Ana", usuario.Nome);
        Assert.Equal("ana@email.com", usuario.Email);
        Assert.Equal("hash", usuario.SenhaHash);
        Assert.Equal("User", usuario.Role);
    }

    [Theory]
    [InlineData("", "ana@email.com", "hash")]
    [InlineData("   ", "ana@email.com", "hash")]
    [InlineData("Ana", "", "hash")]
    [InlineData("Ana", "ana@email.com", "")]
    public void Construtor_CampoObrigatorioVazio_LancaDomainException(string nome, string email, string senhaHash)
    {
        // Arrange & Act
        var exception = Record.Exception(() => new Usuario(nome, email, senhaHash));

        // Assert
        Assert.IsType<DomainException>(exception);
    }
}