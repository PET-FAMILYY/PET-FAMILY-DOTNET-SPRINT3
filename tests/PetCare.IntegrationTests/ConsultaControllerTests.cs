using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using PetCare.Application.DTOs;
using Xunit;

namespace PetCare.IntegrationTests;

[Collection("IntegrationTests")]
public class ConsultaControllerTests
{
    private readonly HttpClient _client;

    public ConsultaControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<int> CriarPetAsync()
    {
        var tutorRequest = new TutorRequest("Tutor Consulta", $"tutor{Guid.NewGuid():N}@email.com", null);
        var tutorResponse = await _client.PostAsJsonAsync("/api/tutor", tutorRequest);
        var tutor = await tutorResponse.Content.ReadFromJsonAsync<TutorResponse>();

        var petRequest = new PetRequest("Bidu", "Cachorro", null, DateTime.UtcNow.AddYears(-3), tutor!.Id);
        var petResponse = await _client.PostAsJsonAsync("/api/pet", petRequest);
        var pet = await petResponse.Content.ReadFromJsonAsync<PetResponse>();

        return pet!.Id;
    }

    [Fact]
    public async Task Post_PetExistente_Retorna201()
    {
        // Arrange
        var petId = await CriarPetAsync();
        var request = new ConsultaRequest(DateTime.UtcNow.AddDays(1), "Check-up anual", petId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/consulta", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_PetInexistente_Retorna404()
    {
        // Arrange
        var request = new ConsultaRequest(DateTime.UtcNow.AddDays(1), "Check-up anual", 999999);

        // Act
        var response = await _client.PostAsJsonAsync("/api/consulta", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_IdInexistente_Retorna404()
    {
        // Act
        var response = await _client.GetAsync("/api/consulta/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
