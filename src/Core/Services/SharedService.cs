using Core.Abstraction;
using Core.Behavior;
using Core.Configuration;
using Core.Helpers;
using Core.Interfaces;
using Core.Kafka;
using Core.Mediatr.Behavior;
using Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Globalization;

namespace Core.Services;

public static class SharedService
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services, IConfiguration config = null!)
    {
        // Définir la culture par défaut
        var cultureInfo = CultureInfo.GetCultureInfo("fr-FR");
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new DateFrUniversalHelper());
            });

        // Enregistre le service TransactionStatus dans le conteneur d'injection de dépendances.
        // - ITransactionStatus : l'interface qui expose l'état de la transaction (Commit ou Rollback).
        // - TransactionStatus : l'implémentation concrète qui contient la propriété Committed.
        // - AddScoped : chaque requête HTTP (scope) aura sa propre instance de TransactionStatus.
        //   Cela garantit que l'état de la transaction est isolé par requête et ne fuit pas entre utilisateurs.
        //   Exemple : si une requête commit sa transaction, Committed = true uniquement pour ce scope.
        //   Une autre requête parallèle aura sa propre instance avec Committed = false par défaut.
        services.AddScoped<ITransactionStatus, TransactionStatus>();

        // Enregistre le pattern Repository générique pour permettre l'injection de n'importe quelle entité (pour tout les microservices)
        services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));

        // --- SERVICE DE CACHE HYBRIDE (L1 + L2) ---
        // Injection de ton service personnalisé qui orchestre le cache local et distribué.
        // En 'Scoped', il peut être injecté partout où tu as besoin d'optimiser les performances.
        services.AddScoped<IHybridCacheService, HybridCacheService>();

        // --- INFRASTRUCTURE DE CACHE EN MÉMOIRE ---
        // Enregistre l'implémentation standard d'ASP.NET Core (IMemoryCache).
        // Indispensable ici car ton 'HybridCacheService' en dépend pour sa couche L1 (mémoire locale).
        services.AddMemoryCache();

        // --- CONFIGURATION DU CACHE DISTRIBUÉ REDIS ---
        // Utilise la bibliothèque StackExchange.Redis pour connecter l'application à un serveur Redis.
        services.AddStackExchangeRedisCache(options =>
        {
            // Adresse du serveur Redis. Ici, "redis" correspond au nom du service 
            // défini dans ton fichier docker-compose, sur le port par défaut 6379.
            options.Configuration = "redis:6379";
        });

        // On enregistre dans le conteneur d'injection de dépendances (DI)
        // une instance de IConnectionMultiplexer (la connexion Redis).
        // AddSingleton => une seule instance partagée dans toute l'application.
        services.AddSingleton<RedisConnectionService>();
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var factory = sp.GetRequiredService<RedisConnectionService>();
            return factory.CreateConnection();
        });

        // Enregistrement des pipelines behavior Mediatr

        // 0. Metrics : mesure le temps global d'exécution de la requête : on encapsule toute les requete suivantes pour connaitre la durée globale
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MetricsPipelineBehavior<,>));

        // 1. avant tout on log la requete. on commence par logger tout ce qui s'apprête à se produire
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));

        // 2. ensuite on valide la requete, si elle est invalide on ne continue pas et on retourne une erreur. On évite ainsi d'ouvrir une transaction inutilement!
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RequestValidatorPipelineBehavior<,>));

        // 3. ensuite on vérifie si la requete est en cache, si oui on retourne la réponse en cache et on évite ainsi d'exécuter la requete et d'ouvrir une transaction inutilement pour les requetes de lecture
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CachePipelineBehavior<,>));

        // 4. Ensuite on ouvre une transaction, si la requete est valide et que c'est une requete de Command. On évite ainsi d'ouvrir une transaction inutilement pour les requetes de lecture
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>));

        // 5. Enfin on effectue les validations métier, si la requete est valide et que c'est une requete de Command. On évite ainsi d'effectuer des validations métier inutiles pour les requetes de lecture
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(BusinessValidationPipelineBehavior<,>));

        // 6. Après l'exécution de la requete, on invalide le cache si c'est une requete de modification (Create, Update, Delete). On évite ainsi de retourner des données obsolètes
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CacheInvalidationPipelineBehavior<,>));

        // Scanne l’assembly Shared pour trouver tous les AbstractValidator<T> : FluentValidation
        services.AddValidatorsFromAssembly(typeof(SharedService).Assembly);


        // KafKa Setting
        // On lie la section JSON à la classe KafkaSettings
        services.Configure<KafkaSettings>(config.GetSection("KafkaSettings"));

        // On enregistre le Producer en utilisant les réglages injectés
        services.AddSingleton<KafkaProducer>(sp =>
        {
            // On récupère l'instance de configuration via IOptions
            var settings = sp.GetRequiredService<IOptions<KafkaSettings>>().Value;

            // On utilise la propriété BootstrapServers définie dans ton JSON
            return new KafkaProducer(settings);
        });

        // Fin Kafka

        services.Configure<ApiBehaviorOptions>(options =>
        {
            /* 
             * DESACTIVATION DU FILTRE DE VALIDATION AUTOMATIQUE (ModelState)
             * 
             * Par défaut, ASP.NET Core détecte les erreurs de désérialisation JSON (ex: un champ 
             * requis manquant ou un mauvais type) et renvoie immédiatement une réponse 400 de type asp.net core
             * avec un format standard (RFC 7807) AVANT que FluentValidation ne soit exécuté.
             * Notre format de réponse n'est jamais utilisé. Pour éviter ca, il faut juste activer 'SuppressModelStateInvalidFilter'.
             *
             * En passant 'SuppressModelStateInvalidFilter' à 'true' :
             * 1. On empêche le framework de couper la requête prématurément.
             * 2. On force la requête à atteindre votre 'ValidationBehavior' MediatR.
             * 3. Cela permet à votre 'AddProductItemsRequestValidator (FluentValidator)' de prendre la main
             *    et de renvoyer VOS messages personnalisés : "L'identifiant du produit est obligatoire".
             */
            options.SuppressModelStateInvalidFilter = true;
        });

        // 🔑 INDISPENSABLE pour le traçage (Correlation ID) :
        // Enregistre IHttpContextAccessor, le service qui permet d'accéder à la requête HTTP 
        // actuelle (headers, cookies, utilisateur) depuis n'importe où dans l'application.
        //
        // Sans cette ligne : 
        // 1. Serilog.Enrichers.CorrelationId ne pourra pas lire le header "x-correlation-id".
        // 2. Vos logs n'auront pas d'ID de corrélation, rendant le débuggage impossible en prod.
        // 3. Votre GlobalErrorMiddleware ne pourra pas accéder au contexte de diagnostic Serilog.
        services.AddHttpContextAccessor();


        return services;
    }
}
