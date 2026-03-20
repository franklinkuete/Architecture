using Ardalis.Result;
using CommandeApi.Application.Commande;
using CommandeApi.Application.Commande.AddCommande;
using CommandeApi.Application.Commande.AddProductItems;
using CommandeApi.Application.Commande.DeleteCommande;
using CommandeApi.Domain.Interfaces;
using CommandeApi.Infrastructure.Entities;
using CommandeApi.Infrastructure.KafkaBackgroundService;
using CommandeApi.Infrastructure.Mappings;
using CommandeApi.Infrastructure.Repositories;
using Core.Configuration;
using Core.Events;
using Core.Interfaces;
using Core.Kafka;
using FluentValidation;
using Mapster;
using MapsterMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace CommandeApi.Infrastructure;

public static class InjectionDependanceInfrastructure
{
    public static IServiceCollection AddDatabaseDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        // Utiliser GetValue pour la sécurité
        var connectionString = configuration.GetValue<string>("MariaDbSetting:ConnectionString");

        services.AddDbContext<CommandeDbContext>(options =>
        {
            // Avec MySQL.EntityFrameworkCore, on ne passe PAS de ServerVersion.
            // On passe uniquement la chaîne de connexion.
            options.UseMySQL(connectionString!, mysqlOptions =>
            {
                mysqlOptions.MaxBatchSize(100);
                // Note : La gestion du Retry (EnableRetryOnFailure) peut varier 
                // selon la version exacte du package Oracle.
            });
        });
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<CommandeDbContext>());
        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {

        // Enregistrement spécifique pour les commandes.
        // Utilise 'Scoped' pour que l'instance soit unique au sein d'une même requête HTTP.
        services.AddScoped<ICommandeRepository, CommandeRepository>();

        services.AddScoped<IUnitOfWorkCommande, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddMapster(this IServiceCollection services)
    {
        // Configuration globale de Mapster
        var config = TypeAdapterConfig.GlobalSettings;

        // 🔎 Scan de l’assembly Application (où se trouvent tes DTOs / mappings)
        // Ici j’utilise LoginRequest comme point d’ancrage pour récupérer l’assembly
        config.Scan(typeof(CommandeMappingConfiguration).Assembly);

        // Enregistrement de la configuration et du mapper dans le conteneur DI
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }

    public static IServiceCollection AddMediatr(this IServiceCollection services)
    {
        var applicationLayerAssembly = typeof(CommandeRequest).Assembly;
      
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(applicationLayerAssembly));
        return services;
    }


    public static IServiceCollection AddKafkaConsumerEvent(this IServiceCollection services, IConfiguration config = null!)
    {
        // Configurer les settings (depuis appsettings.json)
        services.Configure<KafkaSettings>(config.GetSection("KafkaSettings"));

        // Enregistrer le Consumer typé (Singleton)
        services.AddSingleton<KafkaConsumer<StockDecrementFailedEvent>>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<KafkaSettings>>().Value;
            var specificGroupId = $"{settings.GroupId}-decrementfailed";

            // Récupération du logger via ILoggerFactory
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<KafkaConsumer<StockDecrementFailedEvent>>();

            return new KafkaConsumer<StockDecrementFailedEvent>(settings, specificGroupId, logger);
        });

        // Enregistrer l'ouvrier qui fait tourner la boucle (HostedService)
        services.AddHostedService<CommandeBackgroundService>();

        return services;
    }

    public static IServiceCollection AddRequestValidator(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AddCommandRequestValidator>();
        services.AddValidatorsFromAssemblyContaining<AddProductItemsRequestValidator>();
        return services;
    }

    public static IServiceCollection AddBusinessValidator(this IServiceCollection services)
    {
        services.AddScoped<IBusinessValidation<AddCommandeCommand, Result<CommandeResponse>>, AddCommandeBusinessValidation>();
        services.AddScoped<IBusinessValidation<DeleteCommandeCommand, Result<bool>>, DeleteCommandeBusinessValidation>();
        services.AddScoped< IBusinessValidation<AddProductItemsCommand, Result<CommandeResponse>>, AddProductItemsBuisinessValidation>();

        return services;
    }
    public static IServiceCollection AddMassTransit(this IServiceCollection services)
    {
        // Extraction manuelle des paramètres Kafka depuis le fichier de configuration (appsettings.json)
        // On le fait ici pour pouvoir passer le nom du Topic à la méthode AddProducer plus bas.
        var serviceProvider = services.BuildServiceProvider();
        var settings = serviceProvider.GetRequiredService<IOptions<KafkaSettings>>().Value;

        services.AddMassTransit(x =>
        {
            // OBLIGATOIRE : MassTransit nécessite un "Bus" principal pour fonctionner.
            // Comme on utilise Kafka via un "Rider", on initialise un bus en mémoire (InMemory)
            // qui servira de moteur de base pour démarrer les services MassTransit.
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });

            // Ajout du Rider Kafka : Dans MassTransit, Kafka n'est pas un transport natif 
            // comme RabbitMQ, il est traité comme un "Rider" (un passager attaché au bus).
            x.AddRider(rider =>
            {
                // CONFIGURATION DU PRODUCTEUR :
                // On enregistre CommandeCancelEvent pour qu'il soit envoyé vers le topic spécifié.
                // Cela permet d'injecter ITopicProducer<CommandeCancelEvent> dans vos Handlers/Controllers.
                rider.AddProducer<CommandeCancelEvent>(settings.MassTransitTransaction.ProducerTopic);

                // CONFIGURATION DU BROKER KAFKA :
                rider.UsingKafka((context, k) =>
                {
                    // Définit l'adresse du serveur Kafka (ex: localhost:9092)
                    k.Host(settings.BootstrapServers);

                    // Note : Pour un producteur seul, on ne définit pas de TopicEndpoint ici.
                    // Le TopicEndpoint sert uniquement à la consommation (Service Produit).
                });
            });
        });

        return services;
    }

}
