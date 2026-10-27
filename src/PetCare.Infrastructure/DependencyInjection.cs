using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PetCare.Application.Auditing;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Infrastructure.Mongo;
using PetCare.Infrastructure.Persistence;
using PetCare.Infrastructure.Repositories;
using PetCare.Infrastructure.Security;

namespace PetCare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? oracleConnectionString,
        MongoSettings mongo)
    {
        // ---- Relacional (EF Core + Oracle)
        services.AddDbContext<PetCareContext>(options => options.UseOracle(oracleConnectionString));

        services.AddScoped<IRepository<Tutor>, TutorRepository>();
        services.AddScoped<IRepository<Pet>, PetRepository>();
        services.AddScoped<IRepository<Consulta>, ConsultaRepository>();
        services.AddScoped<IRepository<Lembrete>, LembreteRepository>();

        services.AddScoped<ITutorRepository, TutorRepository>();
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IConsultaRepository, ConsultaRepository>();
        services.AddScoped<ILembreteRepository, LembreteRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        // ---- Segurança
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // ---- NoSQL (MongoDB): cliente é thread-safe e deve ser singleton
        services.AddSingleton(mongo);
        services.AddSingleton<IMongoClient>(_ =>
        {
            var settings = MongoClientSettings.FromConnectionString(mongo.ConnectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3); // falha rápido se o Mongo cair
            return new MongoClient(settings);
        });
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongo.Database));
        services.AddScoped<IAuditLogRepository, MongoAuditLogRepository>();

        return services;
    }
}