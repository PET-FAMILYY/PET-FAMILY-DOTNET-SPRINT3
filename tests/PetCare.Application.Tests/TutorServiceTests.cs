using Moq;
using PetCare.Application.Auditing;
using PetCare.Application.DTOs;
using PetCare.Application.Interfaces;
using PetCare.Application.Services;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using Xunit;

namespace PetCare.Application.Tests;

public class TutorServiceTests
{
    private readonly Mock<ITutorRepository> _repository = new();
    private readonly Mock<IAuditTrail> _audit = new();
    private readonly TutorService _service;

    public TutorServiceTests()
    {
        _service = new TutorService(_repository.Object, _audit.Object);
    }

    [Fact]
    public async Task CriarAsync_EmailDuplicado_LancaConflictExceptionENaoPersiste()
    {
        // Arrange
        _repository.Setup(r => r.ExistsByEmailAsync("joao@email.com", null)).ReturnsAsync(true);
        var request = new TutorRequest("João", "joao@email.com", null);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.CriarAsync(request));
        _repository.Verify(r => r.AddAsync(It.IsAny<Tutor>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_EmailNovo_PersisteERegistraAuditoria()
    {
        // Arrange
        _repository.Setup(r => r.ExistsByEmailAsync("novo@email.com", null)).ReturnsAsync(false);
        _repository.Setup(r => r.AddAsync(It.IsAny<Tutor>())).ReturnsAsync((Tutor t) => t);
        var request = new TutorRequest("Novo", "novo@email.com", "11999998888");

        // Act
        var resposta = await _service.CriarAsync(request);

        // Assert
        Assert.Equal("novo@email.com", resposta.Email);
        _audit.Verify(a => a.RegistrarAsync("Tutor", It.IsAny<int>(), "Criacao", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_EmailDeOutroTutor_LancaConflictException()
    {
        // Arrange
        var tutor = new Tutor("João", "joao@email.com", null);
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tutor);
        _repository.Setup(r => r.ExistsByEmailAsync("maria@email.com", 1)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(
            () => _service.AtualizarAsync(1, new TutorRequest("João", "maria@email.com", null)));
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Tutor>()), Times.Never);
    }

    [Fact]
    public async Task ObterAsync_TutorInexistente_LancaResourceNotFoundException()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((Tutor?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _service.ObterAsync(42));
    }
}