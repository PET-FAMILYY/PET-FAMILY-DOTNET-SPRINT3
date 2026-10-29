using Microsoft.Extensions.DependencyInjection;
using PetCare.Application.Auditing;
using PetCare.Application.Services;

namespace PetCare.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITutorService, TutorService>();
        services.AddScoped<IPetService, PetService>();
        services.AddScoped<IConsultaService, ConsultaService>();
        services.AddScoped<ILembreteService, LembreteService>();
        services.AddScoped<IAuditTrail, AuditTrail>();
        services.AddScoped<IAuthService, AuthService>();    
        return services;
    }
}