using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PetCare.API.HealthChecks;
using Xunit;

namespace PetCare.IntegrationTests;

public class ExternalServiceHealthCheckTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => handler(request, ct);
    }

    private sealed class FakeFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private static ExternalServiceHealthCheck Criar(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> resposta)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HealthChecks:ExternalService:Url"] = "https://externo.test/ping",
            ["HealthChecks:ExternalService:TimeoutSeconds"] = "1"
        }).Build();

        return new ExternalServiceHealthCheck(new FakeFactory(new FakeHandler(resposta)), config);
    }

    [Fact]
    public async Task CheckHealthAsync_ServicoResponde200_RetornaHealthy()
    {
        // Arrange
        var check = Criar((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, resultado.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ServicoResponde500_RetornaDegraded()
    {
        // Arrange
        var check = Criar((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Degraded, resultado.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ServicoLancaExcecao_RetornaDegradedSemPropagar()
    {
        // Arrange
        var check = Criar((_, _) => throw new HttpRequestException("sem rede"));

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Degraded, resultado.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ServicoNaoRespondeNoTimeout_RetornaDegraded()
    {
        // Arrange
        var check = Criar(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct); // respeita o timeout do health check
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Degraded, resultado.Status);
    }
}