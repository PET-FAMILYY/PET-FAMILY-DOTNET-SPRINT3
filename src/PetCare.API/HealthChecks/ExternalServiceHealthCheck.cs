using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PetCare.API.HealthChecks;

/// <summary>
/// Verifica a disponibilidade de um serviço externo (HTTP) do qual a API depende
/// ou que serve de referência de conectividade de saída (ex.: ViaCEP para endereço de tutores).
/// Falha do serviço externo reporta Degraded: a API continua operando, mas o problema fica visível.
/// </summary>
public sealed class ExternalServiceHealthCheck : IHealthCheck
{
    public const string HttpClientName = "external-health";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _url;
    private readonly TimeSpan _timeout;

    public ExternalServiceHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _url = configuration["HealthChecks:ExternalService:Url"] ?? "https://viacep.com.br/ws/01001000/json/";
        var seconds = int.TryParse(configuration["HealthChecks:ExternalService:TimeoutSeconds"], out var s) ? s : 3;
        _timeout = TimeSpan.FromSeconds(seconds);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"Serviço externo respondeu {(int)response.StatusCode}.")
                : HealthCheckResult.Degraded($"Serviço externo respondeu {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded($"Serviço externo não respondeu em {_timeout.TotalSeconds:0}s.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Serviço externo indisponível.", ex);
        }
    }
}