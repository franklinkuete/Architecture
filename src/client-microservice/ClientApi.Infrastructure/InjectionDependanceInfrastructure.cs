using Ardalis.Result;
using ClientApi.Application.Client;
using ClientApi.Application.Client.AddClient;
using ClientApi.Application.Client.DeleteClient;
using ClientApi.Domain.Interfaces;
using ClientApi.Infrastructure.Entities;
using ClientApi.Infrastructure.Repository;
using Core.Interfaces;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace ClientApi.Infrastructure;

public static class InjectionDependanceInfrastructure
{
    public static IServiceCollection AddDatabaseDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        // Récupération de la chaîne de connexion depuis le appsettings.json
        string connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // 1. Enregistrement de l'intercepteur dans le conteneur de dépendances (DI).
        // On utilise 'Scoped' pour qu'il ait la même durée de vie que la requête HTTP.
        // Cela permettra plus tard d'y injecter des services comme 'ICurrentUser' ou un Logger.
        services.AddScoped<ClientSaveChangesInterceptor>();

        // 2. Configuration du DbContext spécifique (ClientDbContext).
        // L'utilisation de la surcharge avec (sp, options) est cruciale :
        // 'sp' (IServiceProvider) nous permet d'accéder au conteneur de services pour résoudre l'intercepteur.
        services.AddDbContext<ClientDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);

            // On injecte l'instance de l'intercepteur gérée par .NET.
            // Cela garantit que l'intercepteur bénéficie lui aussi de l'injection de dépendances.
            var interceptor = sp.GetRequiredService<ClientSaveChangesInterceptor>();
            options.AddInterceptors(interceptor);
        });

        // 3. LE PONT (Forwarding) : Résolution de l'abstraction DbContext.
        // Votre 'RepositoryBase' demande un 'DbContext' générique dans son constructeur.
        // Cette ligne dit à .NET : "Si quelqu'un demande un DbContext, ne crée pas une nouvelle instance vide,
        // va plutôt chercher le 'ClientDbContext' que l'on vient de configurer avec ses intercepteurs."
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<ClientDbContext>());

        return services;
    }


    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IUnitOfWorkClient, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddMapster(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration globale de Mapster
        var config = TypeAdapterConfig.GlobalSettings;

        // 🔎 Scan de l’assembly Application (où se trouvent tes DTOs / mappings)
        // Ici j’utilise LoginRequest comme point d’ancrage pour récupérer l’assembly
        config.Scan(typeof(ClientRequest).Assembly);

        // Enregistrement de la configuration et du mapper dans le conteneur DI
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }

    public static IServiceCollection AddMediatr(this IServiceCollection services)
    {
        // 🔎 Récupère explicitement l’assembly de la couche Application
        var applicationAssembly = typeof(ClientRequest).Assembly;

        // ✅ Enregistre MediatR et scanne les handlers dans cet assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));

        return services;
    }

    public static IServiceCollection AddRequestValidator(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AddClientRequestValidator>();

        services.AddValidatorsFromAssemblyContaining<DeleteClientRequestValidator>();

        return services;
    }

    public static IServiceCollection AddBusinessValidator(this IServiceCollection services)
    {
        services.AddScoped<IBusinessValidation<AddClientCommand, Result<ClientResponse>>, AddClientCommandBusinessValidation>();

        return services;
    }

}
