using Ardalis.Result;
using Confluent.Kafka;
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
using ProductApi.Application.Categorie.DeleteCategorie;
using ProductApi.Application.Events;
using ProductApi.Application.Product;
using ProductApi.Application.Product.AddProduct;
using ProductApi.Application.Product.UpdateProduct;
using ProductApi.Domain.Interfaces;
using ProductApi.Infrastructure.Entities;
using ProductApi.Infrastructure.KafkaBackgroundService;
using ProductApi.Infrastructure.Mappings;
using ProductApi.Infrastructure.Repositories;


namespace ProductApi.Infrastructure;

public static class InjectionDependanceInfrastructure
{
    public static IServiceCollection AddDatabaseDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")!;

        services.AddScoped<ProductSaveChangesInterceptor>();

        services.AddDbContext<ProductDbContext>((sp, options) =>
        {
            options.UseMySQL(connectionString);

            var interceptor = sp.GetRequiredService<ProductSaveChangesInterceptor>();
            options.AddInterceptors(interceptor);
        });

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<ProductDbContext>());

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        // Ajout de mes repos
        services.AddScoped<IUnitOfWorkProduct, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategorieRepository, CategorieRepository>();

        return services;
    }

    public static IServiceCollection AddMapster(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(ProductMappingConfiguration).Assembly);

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }

    public static IServiceCollection AddMediatr(this IServiceCollection services)
    {
        var apiAssembly = typeof(AddProductRequest).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(apiAssembly));
        return services;
    }

    public static IServiceCollection AddKafkaConsumerEvent(this IServiceCollection services, IConfiguration config = null!)
    {
        // Chargement de la configuration de base (Serveurs, GroupId par défaut, etc.)
        services.Configure<KafkaSettings>(config.GetSection("KafkaSettings"));

        /* 
         * POURQUOI DES GROUP-IDS DIFFÉRENTS ?
         * Dans Kafka, un GroupId représente une "unité logique" de traitement. 
         * Si deux consommateurs partagent le même GroupId sur un même topic, Kafka distribue 
         * les messages entre eux (équilibrage de charge). L'un recevrait la "Création" 
         * et l'autre "l'Ajout d'items" de manière aléatoire.
         *
         * En créant des GroupIds uniques (suffixes -created et -items-added), on force 
         * Kafka à diffuser CHAQUE message à CHAQUE consommateur (Fan-out). 
         * Ainsi, chaque message est envoyé aux deux instances, et chacune décide 
         * de le traiter ou de l'ignorer selon le type JSON reçu.
         */

        // 1. Consommateur dédié aux événements de CRÉATION de commande
        services.AddSingleton<KafkaConsumer<CommandeCreatedEvent>>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<KafkaSettings>>().Value;
            // On crée un groupe spécifique pour ne pas entrer en compétition avec l'autre consommateur
            var specificGroupId = $"{settings.GroupId}-created";

            // Récupération du logger via ILoggerFactory
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<KafkaConsumer<CommandeCreatedEvent>>();

            return new KafkaConsumer<CommandeCreatedEvent>(settings, specificGroupId,logger);
        });

        // 2. Consommateur dédié aux événements d'AJOUT D'ARTICLES
        services.AddSingleton<KafkaConsumer<CommandeItemsAddedEvent>>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<KafkaSettings>>().Value;
            // On crée un second groupe distinct : Kafka enverra donc une COPIE de chaque message ici aussi
            var specificGroupId = $"{settings.GroupId}-items-added";

            // Récupération du logger via ILoggerFactory
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<KafkaConsumer<CommandeItemsAddedEvent>>();

            return new KafkaConsumer<CommandeItemsAddedEvent>(settings, specificGroupId, logger);
        });

        // L'ouvrier (BackgroundService) qui pilotera ces deux boucles en parallèle
        services.AddHostedService<ProductBackgroundService>();

        return services;
    }

    public static IServiceCollection AddBusinessValidator(this IServiceCollection services, IConfiguration config = null!)
    {
        services.AddScoped<IBusinessValidation<AddProductCommand, Result<ProductResponse>>, AddProductBusinessValidation>();
        services.AddScoped<IBusinessValidation<UpdateProductCommand, Result<ProductResponse>>, UpdateProductBusinessValidator>();
        services.AddScoped<IBusinessValidation<DeleteCategorieCommand, Result<bool?>>, DeleteCategorieBusinessValidation>();

        return services;
    }

    public static IServiceCollection AddRequestValidator(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AddProductRequestValidator>();

        return services;
    }
    public static IServiceCollection AddMassTransit(this IServiceCollection services)
    {
        // 1. RÉCUPÉRATION DES PARAMÈTRES :
        // On extrait les réglages Kafka (BootstrapServers, GroupId, Topic) depuis appsettings.json.
        // Il est préférable de le faire ici pour simplifier la configuration à l'intérieur du bloc AddMassTransit.
        var serviceProvider = services.BuildServiceProvider();
        var settings = serviceProvider.GetRequiredService<IOptions<KafkaSettings>>().Value;

        services.AddMassTransit(x =>
        {
            // BUS PRINCIPAL (InMemory) :
            // MassTransit nécessite un bus "hôte". Comme on utilise Kafka en Rider, 
            // on initialise un transport en mémoire qui servira de moteur de démarrage.
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });

            // CONFIGURATION DU RIDER KAFKA (Consommateur) :
            x.AddRider(rider =>
            {
                // 2. ENREGISTREMENT DU CONSUMER :
                // On déclare la classe qui contient la logique métier (Consume).
                // ATTENTION : On l'ajoute dans le Rider, et PAS dans le bus principal (x.AddConsumer).
                rider.AddConsumer<CommandeCancelledConsumer>();

                rider.UsingKafka((context, k) =>
                {
                    // Définit l'adresse du broker Kafka (ex: localhost:9092)
                    k.Host(settings.BootstrapServers);

                    // 3. CONFIGURATION DU POINT D'ENTRÉE (TOPIC ENDPOINT) :
                    // On lie un type de message (CommandeCancelEvent) à un Topic Kafka spécifique.
                    k.TopicEndpoint<CommandeCancelEvent>(
                        settings.MassTransitTransaction.ProducerTopic, // Nom du topic à écouter
                        settings.GroupId,                             // ID du groupe de consommateurs (crucial pour Kafka)
                        e =>
                        {
                            // GESTION DE L'OFFSET :
                            // AutoOffsetReset.Earliest permet de lire les messages qui auraient été 
                            // envoyés pendant que ce microservice était éteint (si le GroupId est nouveau).
                            e.AutoOffsetReset = AutoOffsetReset.Earliest;

                            // LIAISON FINALE :
                            // On connecte explicitement le consommateur enregistré plus haut à ce topic.
                            e.ConfigureConsumer<CommandeCancelledConsumer>(context);
                        });
                });
            });
        });

        return services;
    }
}
