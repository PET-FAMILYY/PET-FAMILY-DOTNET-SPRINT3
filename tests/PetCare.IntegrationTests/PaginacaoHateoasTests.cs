using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PetCare.Application.Auditing;
using PetCare.Application.Common;
using PetCare.Application.DTOs;
using Xunit;

namespace PetCare.IntegrationTests;

[Collection("IntegrationTests")]
public class PaginacaoHateoasTests
{
    private readonly HttpClient _client;

    public PaginacaoHateoasTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    private async Task<int> CriarTutorAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/tutor",
            new TutorRequest("Tutor Paginação", $"pag{Guid.NewGuid():N}@email.com", null));
        return (await response.Content.ReadFromJsonAsync<TutorResponse>())!.Id;
    }

    private async Task<PetResponse> CriarPetAsync(int tutorId, string nome, string especie = "Cachorro")
    {
        var response = await _client.PostAsJsonAsync("/api/pet",
            new PetRequest(nome, especie, null, DateTime.UtcNow.AddYears(-1), tutorId));
        return (await response.Content.ReadFromJsonAsync<PetResponse>())!;
    }

    [Fact]
    public async Task GetAll_ComPaginacaoEOrdenacao_RetornaPaginaELinksDeNavegacao()
    {
        // Arrange
        var tutorId = await CriarTutorAsync();
        await CriarPetAsync(tutorId, "Amora");
        await CriarPetAsync(tutorId, "Bidu");
        await CriarPetAsync(tutorId, "Cacau");

        // Act
        var response = await _client.GetAsync($"/api/pet?tutorId={tutorId}&page=1&pageSize=2&sortBy=nome&sortDir=desc");
        var pagina = await response.Content.ReadFromJsonAsync<PagedResult<PetResponse>>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(pagina);
        Assert.Equal(new[] { "Cacau", "Bidu" }, pagina!.Items.Select(p => p.Nome));
        Assert.Equal(3, pagina.TotalCount);
        Assert.Equal(2, pagina.TotalPages);
        Assert.Contains(pagina.Links, l => l.Rel == "next" && l.Href.Contains("page=2") && l.Href.Contains($"tutorId={tutorId}"));
        Assert.DoesNotContain(pagina.Links, l => l.Rel == "prev");
    }

    [Fact]
    public async Task GetAll_FiltroPorEspecie_RetornaSomenteOsPetsDaEspecie()
    {
        // Arrange
        var tutorId = await CriarTutorAsync();
        await CriarPetAsync(tutorId, "Rex", "Cachorro");
        await CriarPetAsync(tutorId, "Mia", "Gato");

        // Act
        var pagina = await _client.GetFromJsonAsync<PagedResult<PetResponse>>(
            $"/api/pet?tutorId={tutorId}&especie=gato");

        // Assert
        Assert.NotNull(pagina);
        Assert.Single(pagina!.Items);
        Assert.Equal("Mia", pagina.Items[0].Nome);
    }

    [Fact]
    public async Task GetAll_CampoDeOrdenacaoInvalido_Retorna400()
    {
        // Act
        var response = await _client.GetAsync("/api/pet?sortBy=campoQueNaoExiste");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_RetornaLinksHateoas()
    {
        // Arrange
        var tutorId = await CriarTutorAsync();
        var pet = await CriarPetAsync(tutorId, "Rex");

        // Act
        var resposta = await _client.GetFromJsonAsync<PetResponse>($"/api/pet/{pet.Id}");

        // Assert
        Assert.NotNull(resposta);
        var rels = resposta!.Links.Select(l => l.Rel).ToList();
        Assert.Contains("self", rels);
        Assert.Contains("update", rels);
        Assert.Contains("delete", rels);
        Assert.Contains("tutor", rels);
        Assert.Contains("consultas", rels);
        Assert.Equal($"/api/pet/{pet.Id}", resposta.Links.First(l => l.Rel == "self").Href);
    }

    [Fact]
    public async Task Auditoria_AposCriarTutor_RegistraEventoConsultavel()
    {
        // Arrange
        var tutorId = await CriarTutorAsync();

        // Act
        var pagina = await _client.GetFromJsonAsync<PagedResult<AuditLogEntry>>(
            $"/api/auditoria?entidade=Tutor&acao=Criacao");

        // Assert
        Assert.NotNull(pagina);
        Assert.Contains(pagina!.Items, e => e.EntidadeId == tutorId && e.Acao == "Criacao");
    }

    [Fact]
    public async Task Health_ComChecksIsolados_Retorna200ComCheckSelf()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", doc.RootElement.GetProperty("status").GetString());
        Assert.Contains(doc.RootElement.GetProperty("checks").EnumerateArray(),
            c => c.GetProperty("name").GetString() == "self");
    }
}