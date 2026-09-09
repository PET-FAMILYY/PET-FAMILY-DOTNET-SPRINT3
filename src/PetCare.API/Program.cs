using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PetCare.API.Middleware;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Infrastructure.Persistence;
using PetCare.Infrastructure.Repositories;
using Serilog;
using Serilog.Enrichers.CorrelationId;

const string ServiceName = "PetCare.API";

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog (logging estruturado, console + arquivo, correlação por requisição) ----------
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .Enrich.WithProperty("Application", ServiceName)
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/petcare-api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({CorrelationId}) {Message:lj}{NewLine}{Exception}"));

// ---------- Serviços da aplicação ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PetCare API",
        Version = "v1",
        Description = "API para gestão de clínica veterinária / cuidado de pets (tutores, pets, consultas e lembretes)."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ---------- Persistência (EF Core + Oracle) ----------
var connectionString = builder.Configuration.GetConnectionString("OracleConnection");
builder.Services.AddDbContext<PetCareContext>(options =>
    options.UseOracle(connectionString));

// ---------- Repositórios ----------
builder.Services.AddScoped<IRepository<Tutor>, TutorRepository>();
builder.Services.AddScoped<IRepository<Pet>, PetRepository>();
builder.Services.AddScoped<IRepository<Consulta>, ConsultaRepository>();
builder.Services.AddScoped<IRepository<Lembrete>, LembreteRepository>();

builder.Services.AddScoped<ITutorRepository, TutorRepository>();
builder.Services.AddScoped<IPetRepository, PetRepository>();
builder.Services.AddScoped<IConsultaRepository, ConsultaRepository>();
builder.Services.AddScoped<ILembreteRepository, LembreteRepository>();

// ---------- Tratamento global de exceções (RFC 7807 / ProblemDetails) ----------
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ---------- Health Checks ----------
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API no ar."), tags: new[] { "live" })
    .AddOracle(
        connectionString ?? string.Empty,
        name: "oracle_database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "ready", "db" });

// ---------- OpenTelemetry (tracing + métricas Prometheus em /metrics) ----------
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(ServiceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

// ---------- Pipeline ----------
app.UseExceptionHandler(_ => { }); // delega para o GlobalExceptionHandler registrado acima

app.UseSerilogRequestLogging();

// Workaround: a UI embutida do Swashbuckle 10.x nao reconhece o patch "3.0.4"
// do OpenAPI (bug conhecido do swagger-ui, ver 42Crunch/vscode-openapi#311).
// Reescreve a resposta do swagger.json trocando 3.0.4 -> 3.0.3 antes de servir.
app.Use(async (context, next) =>
{
    if (context.Request.Path.Value?.EndsWith("swagger.json") == true)
    {
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await next();

        buffer.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(buffer).ReadToEndAsync();
        json = json.Replace("\"openapi\":\"3.0.4\"", "\"openapi\":\"3.0.3\"")
                    .Replace("\"openapi\": \"3.0.4\"", "\"openapi\": \"3.0.3\"");

        context.Response.Body = originalBody;
        context.Response.ContentLength = System.Text.Encoding.UTF8.GetByteCount(json);
        await context.Response.WriteAsync(json);
    }
    else
    {
        await next();
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PetCare API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// GET /health com detalhamento por check em JSON
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteResponseAsync
});

// GET /metrics compatível com Prometheus
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

// Necessário para os testes de integração (WebApplicationFactory<Program>)
public partial class Program { }
