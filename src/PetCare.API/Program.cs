using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PetCare.API.HealthChecks;
using PetCare.API.Middleware;
using PetCare.API.Services;
using PetCare.Application;
using PetCare.Application.Auditing;
using PetCare.Infrastructure;
using PetCare.Infrastructure.HealthChecks;
using PetCare.Infrastructure.Mongo;
using Serilog;
using Microsoft.EntityFrameworkCore;
using PetCare.Infrastructure.Persistence;

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

// ---------- MVC / Swagger ----------
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PetCare API",
        Version = "v1",
        Description = "API para gestão de clínica veterinária / cuidado de pets (tutores, pets, consultas, lembretes e auditoria). " +
                      "Listagens com paginação (page, pageSize), ordenação (sortBy, sortDir) e filtros; respostas com links HATEOAS."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Informe apenas o token JWT retornado por POST /api/auth/login (sem o prefixo 'Bearer')."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>(Array.Empty<string>())
    });
});

// ---------- Camadas (Clean Architecture): cada camada registra suas dependências ----------
var oracleConnection = builder.Configuration.GetConnectionString("OracleConnection");
var mongoSettings = builder.Configuration.GetSection("Mongo").Get<MongoSettings>() ?? new MongoSettings();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(oracleConnection, mongoSettings);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// ---------- Tratamento global de exceções (RFC 7807 / ProblemDetails) ----------
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    // Garante traceId também nos ProblemDetails gerados pelo framework (validação, 401, 404...)
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});

// ---------- Health Checks ----------
builder.Services.AddHttpClient(ExternalServiceHealthCheck.HttpClientName);
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API no ar."), tags: new[] { "live" })
    .AddOracle(
        oracleConnection ?? string.Empty,
        name: "oracle_database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "db" })
    .AddCheck<MongoHealthCheck>(
        "mongodb",
        failureStatus: HealthStatus.Degraded,   // auditoria é best-effort: Mongo fora não derruba a API
        tags: new[] { "ready", "db" })
    .AddCheck<ExternalServiceHealthCheck>(
        "external_service",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "ready", "external" });

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

// ---------- Autenticação JWT ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key não configurada.");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>().CreateLogger("JwtBearer");
                logger.LogWarning("Falha na autenticação JWT: {Tipo} - {Mensagem}",
                    context.Exception.GetType().Name, context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>().CreateLogger("JwtBearer");
                logger.LogWarning("Desafio JWT: {Erro} - {Descricao}", context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PetCareContext>();
    await db.Database.MigrateAsync();
}

// ---------- Pipeline ----------
app.UseExceptionHandler(_ => { }); // delega para o GlobalExceptionHandler registrado acima
app.UseStatusCodePages(); // 401/403/404 sem corpo passam a sair como ProblemDetails
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
        context.Response.ContentLength = Encoding.UTF8.GetByteCount(json);
        await context.Response.WriteAsync(json);
    }
    else
    {
        await next();
    }
});

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PetCare API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// GET /health        -> todos os checks (JSON detalhado)
// GET /health/live   -> apenas "a API está de pé" (liveness)
// GET /health/ready  -> dependências: Oracle, MongoDB e serviço externo (readiness)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteResponseAsync
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponseAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponseAsync
});

// GET /metrics compatível com Prometheus
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

// Necessário para os testes de integração (WebApplicationFactory<Program>)
public partial class Program { }