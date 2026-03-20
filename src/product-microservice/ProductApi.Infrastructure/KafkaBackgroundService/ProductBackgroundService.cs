using Core.Configuration;
using Core.Events;
using Core.Kafka;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductApi.Application.Product.UpdateStock;

namespace ProductApi.Infrastructure.KafkaBackgroundService;

/// <summary>
/// Service d'arrière-plan (Worker) qui fait le pont entre Kafka et la logique métier de l'application.
/// S'exécute pendant toute la durée de vie de l'application.
/// </summary>
public class ProductBackgroundService : BackgroundService
{
    private readonly KafkaConsumer<CommandeCreatedEvent> _consumerCommandeCreated;
    private readonly KafkaConsumer<CommandeItemsAddedEvent> _consumerItemsAdded;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaSettings _settings;
    private readonly ILogger<ProductBackgroundService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public ProductBackgroundService(
        KafkaConsumer<CommandeCreatedEvent> consumerCommandeCreated,
        KafkaConsumer<CommandeItemsAddedEvent> consumerItemsAdded,
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaSettings> settings,
        ILogger<ProductBackgroundService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _consumerCommandeCreated = consumerCommandeCreated;
        _scopeFactory = scopeFactory; // Nécessaire pour créer des services Scoped (DB, Mediator)
        _settings = settings.Value;
        _consumerItemsAdded = consumerItemsAdded;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Point d'entrée principal du service lors du démarrage de l'application.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerTopic = _settings.KafkaTransaction.ConsumerTopic;

        // 1. On DÉMARRE les tâches sans faire de 'await' immédiatement.
        // Cela permet de lancer les boucles de consommation en arrière-plan.

        // Tâche pour le premier consommateur
        var taskCommandeCreated = _consumerCommandeCreated.ConsumeAsync(consumerTopic, async (eventMessage) =>
        {
            _logger.LogInformation("{prefixKafka} 📨 Kafka : Mise à jour du stock produit en cours...sur un evenemnt produit par le microservice Commande : {CommandeId} avec TraceId : {traceId}", Constante.Prefix.KafkaPrefix, eventMessage.CommandeId, _httpContextAccessor?.HttpContext?.TraceIdentifier);
            _logger.LogInformation("{prefixKafka} Message Kafka reçu sur le Topic {topic} à la création d'une commande (avec produits) pour la commande CommandeId: {CommandeId} avec la TraceId : {traceId}", Constante.Prefix.KafkaPrefix, consumerTopic, eventMessage.CommandeId, _httpContextAccessor?.HttpContext?.TraceIdentifier);
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new UpdateStockCommandeCreatedCommand(eventMessage);
            _logger.LogInformation("{prefixKafka} 📨 Sending UpdateStockCommandeCreatedCommand for CommandeId: {CommandeId} with TraceId : {traceId}", Constante.Prefix.KafkaPrefix, eventMessage.CommandeId, _httpContextAccessor?.HttpContext?.TraceIdentifier);
            await mediator.Send(command, stoppingToken);
        }, stoppingToken);

        var taskItemsAdded = _consumerItemsAdded.ConsumeAsync(consumerTopic, async (eventMessage) =>
        {
            _logger.LogInformation("{prefixKafka} 📨 Kafka : Mise à jour du stock produit en cours...sur un evenemnt produit par le microservice Commande : {CommandeId} avec TraceId : {traceId}",
                Constante.Prefix.KafkaPrefix, eventMessage.CommandeId,
                _httpContextAccessor?.HttpContext?.TraceIdentifier);

            _logger.LogInformation("{prefixKafka} 📨 Message Kafka reçu sur le Topic {topic}  pour la commande CommandeId: {CommandeId} , {AddedCount} élément(s) en cours d'ajout",
                Constante.Prefix.KafkaPrefix, consumerTopic,
                eventMessage.CommandeId,
                eventMessage.AddedProductList.Count);

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Transformation en CommandeCreatedEvent pour réutiliser le même UpdateStockCommandeCreatedCommand
            var ItemsAdded = new CommandeCreatedEvent(products: eventMessage.AddedProductList, CommandeId: eventMessage.CommandeId);

            var command = new UpdateStockCommandeCreatedCommand(ItemsAdded);

            _logger.LogInformation("{prefixKafka} 📨 Envoi du message à UpdateStockCommandeCreatedCommand  sur le topic {topic} pour la commande: {CommandeId} pour mise à jour des stock produit", Constante.Prefix.KafkaPrefix, consumerTopic, eventMessage.CommandeId);
            await mediator.Send(command, stoppingToken);

        }, stoppingToken);

        // 3. On utilise Task.WhenAll pour maintenir le BackgroundService "en vie"
        // tant que l'une des tâches de consommation tourne.
        // Si l'une des tâches crash ou si le stoppingToken est annulé, WhenAll réagira.
        await Task.WhenAll(taskCommandeCreated, taskItemsAdded);
    }


    // Remarque : La méthode Dispose() n'est pas surchargée ici car le 'KafkaConsumer' 
    // est enregistré en tant que Singleton dans l'injection de dépendances (DI).
    // C'est donc le conteneur de services .NET qui s'occupera de le libérer proprement.
}
