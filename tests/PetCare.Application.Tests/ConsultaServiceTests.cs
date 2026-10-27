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

public class ConsultaServiceTests
{
    private readonly Mock<IConsultaRepository> _repository = new();
    private readonly Mock<IAuditTrail> _audit = new();
    private readonly ConsultaService _service;

    public ConsultaServiceTests()
    {
        _service = new ConsultaService(_repository.Object, _audit.Object);
    }

    [Fact]
    public async Task CriarAsync_DadosValidos_PersisteERegistraAuditoria()
    {
        // Arrange
        _repository.Setup(r => r.AddAsync(It.IsAny<Consulta>())).ReturnsAsync((Consulta c) => c);
        var request = new ConsultaRequest(DateTime.UtcNow.AddDays(3), "Rotina", 1);

        // Act
        var resposta = await _service.CriarAsync(request);

        // Assert
        Assert.Equal(1, resposta.PetId);
        _repository.Verify(r => r.AddAsync(It.IsAny<Consulta>()), Times.Once);
        _audit.Verify(a => a.RegistrarAsync("Consulta", It.IsAny<int>(), "Criacao", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ObterAsync_ConsultaInexistente_LancaResourceNotFoundException()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Consulta?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _service.ObterAsync(99));
    }

    [Fact]
    public async Task RemoverAsync_ConsultaExistente_RemoveERegistraAuditoria()
    {
        // Arrange
        var consulta = new Consulta(DateTime.UtcNow.AddDays(3), null, 1);
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(consulta);

        // Act
        await _service.RemoverAsync(1);

        // Assert
        _repository.Verify(r => r.DeleteAsync(consulta), Times.Once);
        _audit.Verify(a => a.RegistrarAsync("Consulta", 1, "Remocao", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ListarAsync_MapeiaPaginaDoRepositorio()
    {
        // Arrange
        var consulta = new Consulta(DateTime.UtcNow.AddDays(3), "Rotina", 1);
        var filtro = new ConsultaFilter { Page = 1, PageSize = 10, PetId = 1 };
        _repository
            .Setup(r => r.GetPagedAsync(filtro, It.IsAny<System.Linq.Expressions.Expression<Func<Consulta, bool>>?>()))
            .ReturnsAsync(new PagedResult<Consulta>(new[] { consulta }, 1, 10, 1));

        // Act
        var pagina = await _service.ListarAsync(filtro);

        // Assert
        Assert.Single(pagina.Items);
        Assert.Equal("Rotina", pagina.Items[0].Observacoes);
    }
}