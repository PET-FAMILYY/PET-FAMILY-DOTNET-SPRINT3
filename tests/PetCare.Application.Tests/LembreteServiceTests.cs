using System.Linq.Expressions;
using Moq;
using PetCare.Application.Auditing;
using PetCare.Application.Common;
using PetCare.Application.DTOs;
using PetCare.Application.Interfaces;
using PetCare.Application.Services;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using Xunit;

namespace PetCare.Application.Tests;

public class LembreteServiceTests
{
    private readonly Mock<ILembreteRepository> _repository = new();
    private readonly Mock<IPetRepository> _pets = new();
    private readonly Mock<IAuditTrail> _audit = new();
    private readonly LembreteService _service;

    public LembreteServiceTests()
    {
        _service = new LembreteService(_repository.Object, _pets.Object , _audit.Object);
    }

    [Fact]
    public async Task CriarAsync_DadosValidos_PersisteERegistraAuditoria()
    {
        // Arrange
        _pets.Setup(r => r.ExistsByIdAsync(1)).ReturnsAsync(true);
        _repository.Setup(r => r.AddAsync(It.IsAny<Lembrete>())).ReturnsAsync((Lembrete l) => l);
        var request = new LembreteRequest("Vacina", "Antirrábica", DateTime.UtcNow.AddDays(5), 1);

        // Act
        var resposta = await _service.CriarAsync(request);

        // Assert
        Assert.Equal("Vacina", resposta.Titulo);
        Assert.Equal(1, resposta.PetId);
        _repository.Verify(r => r.AddAsync(It.IsAny<Lembrete>()), Times.Once);
        _audit.Verify(a => a.RegistrarAsync("Lembrete", It.IsAny<int>(), "Criacao", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_DataNoPassado_LancaDomainExceptionENaoPersiste()
    {
        // Arrange
        var request = new LembreteRequest("Vacina", null, DateTime.UtcNow.AddDays(-5), 1);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _service.CriarAsync(request));
        _repository.Verify(r => r.AddAsync(It.IsAny<Lembrete>()), Times.Never);
        _audit.Verify(a => a.RegistrarAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_PetInexistente_LancaResourceNotFoundExceptionENaoPersiste()
    {
        // Arrange
        _pets.Setup(r => r.ExistsByIdAsync(999)).ReturnsAsync(false);
        var request = new LembreteRequest("Vacina", "Reforço anual", DateTime.UtcNow.AddDays(2), 999);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _service.CriarAsync(request));
        _repository.Verify(r => r.AddAsync(It.IsAny<Lembrete>()), Times.Never);
        _audit.Verify(a => a.RegistrarAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }
    
    [Fact]
    public async Task ObterAsync_LembreteInexistente_LancaResourceNotFoundException()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Lembrete?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _service.ObterAsync(99));
    }

    [Fact]
    public async Task AtualizarAsync_LembreteExistente_AtualizaERegistraAuditoria()
    {
        // Arrange
        var lembrete = new Lembrete("Vacina", null, DateTime.UtcNow.AddDays(5), 1);
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lembrete);
        var request = new LembreteRequest("Retorno", "Consulta de retorno", DateTime.UtcNow.AddDays(10), 1);

        // Act
        var resposta = await _service.AtualizarAsync(1, request);

        // Assert
        Assert.Equal("Retorno", resposta.Titulo);
        _repository.Verify(r => r.UpdateAsync(lembrete), Times.Once);
        _audit.Verify(a => a.RegistrarAsync("Lembrete", It.IsAny<int>(), "Atualizacao", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_LembreteExistente_RemoveERegistraAuditoria()
    {
        // Arrange
        var lembrete = new Lembrete("Vacina", null, DateTime.UtcNow.AddDays(5), 1);
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lembrete);

        // Act
        await _service.RemoverAsync(1);

        // Assert
        _repository.Verify(r => r.DeleteAsync(lembrete), Times.Once);
        _audit.Verify(a => a.RegistrarAsync("Lembrete", 1, "Remocao", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ListarAsync_RepassaFiltroEMapeiaPagina()
    {
        // Arrange
        var lembrete = new Lembrete("Vacina", null, DateTime.UtcNow.AddDays(5), 1);
        var filtro = new LembreteFilter { Page = 1, PageSize = 10, PetId = 1, Titulo = "vac" };
        _repository
            .Setup(r => r.GetPagedAsync(filtro, It.IsAny<Expression<Func<Lembrete, bool>>?>()))
            .ReturnsAsync(new PagedResult<Lembrete>(new[] { lembrete }, 1, 10, 1));

        // Act
        var pagina = await _service.ListarAsync(filtro);

        // Assert
        Assert.Single(pagina.Items);
        Assert.Equal("Vacina", pagina.Items[0].Titulo);
        Assert.Equal(1, pagina.TotalPages);
    }
}