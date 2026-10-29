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

public class PetServiceTests
{
    private readonly Mock<IPetRepository> _repository = new();
    private readonly Mock<ITutorRepository> _tutores = new();
    private readonly Mock<IAuditTrail> _audit = new();
    private readonly PetService _service;

    public PetServiceTests()
    {
        _service = new PetService(_repository.Object, _tutores.Object, _audit.Object);
    }

    [Fact]
    public async Task CriarAsync_DadosValidos_PersistePetERegistraAuditoria()
    {
        // Arrange
        _tutores.Setup(r => r.ExistsByIdAsync(1)).ReturnsAsync(true);
        _repository.Setup(r => r.AddAsync(It.IsAny<Pet>())).ReturnsAsync((Pet p) => p);
        var request = new PetRequest("Rex", "Cachorro", null, DateTime.UtcNow.AddYears(-1), 1);

        // Act
        var resposta = await _service.CriarAsync(request);

        // Assert
        Assert.Equal("Rex", resposta.Nome);
        _repository.Verify(r => r.AddAsync(It.IsAny<Pet>()), Times.Once);
        _audit.Verify(a => a.RegistrarAsync("Pet", It.IsAny<int>(), "Criacao", It.IsAny<string?>()), Times.Once);
    }
    
    [Fact]
    public async Task CriarAsync_TutorInexistente_LancaResourceNotFoundExceptionENaoPersiste()
    {
        // Arrange
        _tutores.Setup(r => r.ExistsByIdAsync(999)).ReturnsAsync(false);
        var request = new PetRequest("Rex", "Cachorro", null, DateTime.UtcNow.AddYears(-1), 999);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _service.CriarAsync(request));
        _repository.Verify(r => r.AddAsync(It.IsAny<Pet>()), Times.Never);
        _audit.Verify(a => a.RegistrarAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }
    

    [Fact]
    public async Task CriarAsync_DataNascimentoFutura_LancaDomainExceptionENaoPersiste()
    {
        // Arrange
        var request = new PetRequest("Rex", "Cachorro", null, DateTime.UtcNow.AddDays(2), 1);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _service.CriarAsync(request));
        _repository.Verify(r => r.AddAsync(It.IsAny<Pet>()), Times.Never);
        _audit.Verify(a => a.RegistrarAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task ObterAsync_PetInexistente_LancaResourceNotFoundException()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Pet?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _service.ObterAsync(99));
    }

    [Fact]
    public async Task AtualizarAsync_PetExistente_AtualizaERegistraAuditoria()
    {
        // Arrange
        var pet = new Pet("Rex", "Cachorro", null, DateTime.UtcNow.AddYears(-2), 1);
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);
        var request = new PetRequest("Thor", "Cachorro", "Labrador", DateTime.UtcNow.AddYears(-2), 1);

        // Act
        var resposta = await _service.AtualizarAsync(1, request);

        // Assert
        Assert.Equal("Thor", resposta.Nome);
        _repository.Verify(r => r.UpdateAsync(pet), Times.Once);
        _audit.Verify(a => a.RegistrarAsync("Pet", It.IsAny<int>(), "Atualizacao", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_PetExistente_RemoveERegistraAuditoria()
    {
        // Arrange
        var pet = new Pet("Rex", "Cachorro", null, DateTime.UtcNow.AddYears(-2), 1);
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pet);

        // Act
        await _service.RemoverAsync(1);

        // Assert
        _repository.Verify(r => r.DeleteAsync(pet), Times.Once);
        _audit.Verify(a => a.RegistrarAsync("Pet", 1, "Remocao", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ListarAsync_RepassaFiltroEMapeiaPagina()
    {
        // Arrange
        var pet = new Pet("Rex", "Cachorro", null, DateTime.UtcNow.AddYears(-1), 1);
        var filtro = new PetFilter { Page = 2, PageSize = 5, Especie = "cachorro" };
        _repository
            .Setup(r => r.GetPagedAsync(filtro, It.IsAny<Expression<Func<Pet, bool>>?>()))
            .ReturnsAsync(new PagedResult<Pet>(new[] { pet }, 2, 5, 6));

        // Act
        var pagina = await _service.ListarAsync(filtro);

        // Assert
        Assert.Single(pagina.Items);
        Assert.Equal("Rex", pagina.Items[0].Nome);
        Assert.Equal(2, pagina.Page);
        Assert.Equal(2, pagina.TotalPages);
    }
}