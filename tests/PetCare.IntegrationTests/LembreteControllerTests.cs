using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using PetCare.Application.DTOs;
using Xunit;

namespace PetCare.IntegrationTests;

public class LembreteControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LembreteControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<int> CriarPetAsync()
    {
        var tutorRequest = new TutorRequest("Tutor Lembrete", $"tutor{Guid.NewGuid():N}@email.com", null);
        var tutorResponse = await _client.PostAsJsonAsync("/api/tutor", tutorRequest);
        var tutor = await tutorResponse.Content.ReadFromJsonAsync<TutorResponse>();

        var petRequest = new PetRequest("Thor", "Cachorro", null, DateTime.UtcNow.AddYears(-2), tutor!.Id);
        var petResponse = await _client.PostAsJsonAsync("/api/pet", petRequest);
        var pet = await petResponse.Content.ReadFromJsonAsync<PetResponse>();

        return pet!.Id;
    }

    [Fact]
    public async Task Post_PetExistente_Retorna201()
    {
        // Arrange
        var petId = await CriarPetAsync();
        var request = new LembreteRequest("Vacina V10", "Aplicar dose de reforço", DateTime.UtcNow.AddDays(2), petId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/lembrete", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_DataLembreteAnteriorAtual_Retorna400()
    {
        // Arrange
        var petId = await CriarPetAsync();
        var request = new LembreteRequest("Vacina V10", "desc", DateTime.UtcNow.AddDays(-1), petId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/lembrete", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_PetInexistente_Retorna404()
    {
        // Arrange
        var request = new LembreteRequest("Vacina V10", "desc", DateTime.UtcNow.AddDays(1), 999999);

        // Act
        var response = await _client.PostAsJsonAsync("/api/lembrete", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
