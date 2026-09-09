using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using PetCare.Application.DTOs;
using Xunit;

namespace PetCare.IntegrationTests;

public class PetControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PetControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<int> CriarTutorAsync()
    {
        var request = new TutorRequest("Tutor de Teste", $"tutor{Guid.NewGuid():N}@email.com", null);
        var response = await _client.PostAsJsonAsync("/api/tutor", request);
        var tutor = await response.Content.ReadFromJsonAsync<TutorResponse>();
        return tutor!.Id;
    }

    [Fact]
    public async Task Post_TutorExistente_Retorna201()
    {
        // Arrange
        var tutorId = await CriarTutorAsync();
        var request = new PetRequest("Rex", "Cachorro", "Vira-lata", DateTime.UtcNow.AddYears(-1), tutorId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/pet", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_TutorInexistente_Retorna404()
    {
        // Arrange
        var request = new PetRequest("Rex", "Cachorro", "Vira-lata", DateTime.UtcNow.AddYears(-1), 999999);

        // Act
        var response = await _client.PostAsJsonAsync("/api/pet", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_DataNascimentoFutura_Retorna400()
    {
        // Arrange
        var tutorId = await CriarTutorAsync();
        var request = new PetRequest("Rex", "Cachorro", null, DateTime.UtcNow.AddDays(1), tutorId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/pet", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_IdInexistente_Retorna404()
    {
        // Act
        var response = await _client.GetAsync("/api/pet/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
