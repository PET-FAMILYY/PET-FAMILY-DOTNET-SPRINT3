using System.Net;
using System.Net.Http.Json;
using PetCare.Application.DTOs;
using Xunit;

namespace PetCare.IntegrationTests;

public class TutorControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TutorControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_SemFiltros_Retorna200()
    {
        // Act
        var response = await _client.GetAsync("/api/tutor");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_DadosValidos_Retorna201ComTutorCriado()
    {
        // Arrange
        var request = new TutorRequest("João Pereira", $"joao{Guid.NewGuid():N}@email.com", "11999998888");

        // Act
        var response = await _client.PostAsJsonAsync("/api/tutor", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var tutor = await response.Content.ReadFromJsonAsync<TutorResponse>();
        Assert.NotNull(tutor);
        Assert.Equal(request.Nome, tutor!.Nome);
    }

    [Fact]
    public async Task Post_NomeVazio_Retorna400()
    {
        // Arrange
        var request = new TutorRequest("", $"invalido{Guid.NewGuid():N}@email.com", null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/tutor", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmailDuplicado_Retorna409()
    {
        // Arrange
        var email = $"duplicado{Guid.NewGuid():N}@email.com";
        var primeiro = new TutorRequest("Primeiro Tutor", email, null);
        await _client.PostAsJsonAsync("/api/tutor", primeiro);

        var segundo = new TutorRequest("Segundo Tutor", email, null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/tutor", segundo);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetById_IdInexistente_Retorna404()
    {
        // Act
        var response = await _client.GetAsync("/api/tutor/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_IdInexistente_Retorna404()
    {
        // Act
        var response = await _client.DeleteAsync("/api/tutor/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
