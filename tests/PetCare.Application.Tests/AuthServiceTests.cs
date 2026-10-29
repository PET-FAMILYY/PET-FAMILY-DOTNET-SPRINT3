using Moq;
using PetCare.Application.DTOs;
using PetCare.Application.Interfaces;
using PetCare.Application.Services;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using Xunit;

namespace PetCare.Application.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_usuarios.Object, _hasher.Object, _jwt.Object);
    }

    [Fact]
    public async Task RegistrarAsync_DadosValidos_PersisteUsuarioComSenhaHasheada()
    {
        // Arrange
        _usuarios.Setup(r => r.ExisteEmailAsync("ana@email.com")).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash("segredo123")).Returns("hash-da-senha");
        var request = new RegisterRequest("Ana", "ana@email.com", "segredo123");

        // Act
        var resposta = await _service.RegistrarAsync(request);

        // Assert
        Assert.Equal("Ana", resposta.Nome);
        Assert.Equal("ana@email.com", resposta.Email);
        _usuarios.Verify(r => r.AdicionarAsync(
            It.Is<Usuario>(u => u.SenhaHash == "hash-da-senha" && u.Email == "ana@email.com")), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_EmailDuplicado_LancaConflictExceptionENaoPersiste()
    {
        // Arrange
        _usuarios.Setup(r => r.ExisteEmailAsync("ana@email.com")).ReturnsAsync(true);
        var request = new RegisterRequest("Ana", "ana@email.com", "segredo123");

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.RegistrarAsync(request));
        _usuarios.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345")]
    public async Task RegistrarAsync_SenhaCurta_LancaDomainExceptionENaoPersiste(string senha)
    {
        // Arrange
        var request = new RegisterRequest("Ana", "ana@email.com", senha);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _service.RegistrarAsync(request));
        _usuarios.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_CredenciaisValidas_RetornaTokenEDadosDoUsuario()
    {
        // Arrange
        var usuario = new Usuario("Ana", "ana@email.com", "hash-da-senha");
        var expiraEm = DateTime.UtcNow.AddHours(1);
        _usuarios.Setup(r => r.ObterPorEmailAsync("ana@email.com")).ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verificar("segredo123", "hash-da-senha")).Returns(true);
        _jwt.Setup(j => j.GerarToken(usuario)).Returns(("token-falso", expiraEm));

        // Act
        var resposta = await _service.LoginAsync(new LoginRequest("ana@email.com", "segredo123"));

        // Assert
        Assert.Equal("token-falso", resposta.Token);
        Assert.Equal(expiraEm, resposta.ExpiraEm);
        Assert.Equal("Ana", resposta.Nome);
        Assert.Equal("User", resposta.Role);
    }

    [Fact]
    public async Task LoginAsync_SenhaIncorreta_LancaAuthenticationFailedException()
    {
        // Arrange
        var usuario = new Usuario("Ana", "ana@email.com", "hash-da-senha");
        _usuarios.Setup(r => r.ObterPorEmailAsync("ana@email.com")).ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verificar("errada", "hash-da-senha")).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _service.LoginAsync(new LoginRequest("ana@email.com", "errada")));
        _jwt.Verify(j => j.GerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_EmailInexistente_LancaAuthenticationFailedException()
    {
        // Arrange
        _usuarios.Setup(r => r.ObterPorEmailAsync("naoexiste@email.com")).ReturnsAsync((Usuario?)null);

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _service.LoginAsync(new LoginRequest("naoexiste@email.com", "qualquer")));
    }
}