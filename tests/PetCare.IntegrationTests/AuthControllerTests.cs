using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PetCare.Application.DTOs;
using Xunit;

namespace PetCare.IntegrationTests;

[Collection("IntegrationTests")]
public class AuthControllerTests
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(); // sem token
    }

    private static string EmailUnico() => $"user{Guid.NewGuid():N}@petcare.com";

    [Fact]
    public async Task Register_DadosValidos_Retorna201()
    {
        // Arrange
        var request = new RegisterRequest("Ana", EmailUnico(), "segredo123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var corpo = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.Equal(request.Email, corpo!.Email);
    }

    [Fact]
    public async Task Register_EmailDuplicado_Retorna409()
    {
        // Arrange
        var request = new RegisterRequest("Ana", EmailUnico(), "segredo123");
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_SenhaCurta_Retorna400()
    {
        // Arrange
        var request = new RegisterRequest("Ana", EmailUnico(), "123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_CredenciaisValidas_Retorna200ComToken()
    {
        // Arrange
        var email = EmailUnico();
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Ana", email, "segredo123"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "segredo123"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var corpo = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(corpo!.Token));
    }

    [Fact]
    public async Task Login_SenhaErrada_Retorna401EmFormatoProblemDetails()
    {
        // Arrange
        var email = EmailUnico();
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Ana", email, "segredo123"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "senha-errada"));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(401, json.RootElement.GetProperty("status").GetInt32());
        Assert.True(json.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task EndpointProtegido_SemToken_Retorna401()
    {
        // Act
        var response = await _client.GetAsync("/api/tutor");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndpointProtegido_ComTokenInvalido_Retorna401()
    {
        // Arrange
        var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/tutor");
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token.invalido.qualquer");

        // Act
        var response = await _client.SendAsync(requisicao);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}