using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PetCare.Domain.Exceptions;

namespace PetCare.API.Middleware;

/// <summary>
/// Converte exceções em respostas RFC 7807 (ProblemDetails) com traceId para correlação com os logs.
/// Erros do cliente (4xx) são logados como Warning; falhas do servidor (5xx) como Error.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = MapException(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception,
                "Erro interno ({StatusCode}) na requisição {TraceId}: {Message}",
                statusCode, httpContext.TraceIdentifier, exception.Message);
        else
            _logger.LogWarning(
                "Requisição rejeitada ({StatusCode}) {TraceId}: {Tipo} - {Message}",
                statusCode, httpContext.TraceIdentifier, exception.GetType().Name, exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError && !_environment.IsDevelopment()
                ? "Ocorreu um erro interno ao processar a requisição."
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Falha de autenticação."),
        ArgumentException => (StatusCodes.Status400BadRequest, "Requisição inválida."),
        DomainException => (StatusCodes.Status400BadRequest, "Regra de negócio violada."),
        ResourceNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado."),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado."),
        ConflictException => (StatusCodes.Status409Conflict, "Conflito de estado."),
        _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor.")
    };
}