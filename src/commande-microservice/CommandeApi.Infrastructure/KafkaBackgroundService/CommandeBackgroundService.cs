using CommandeApi.Application.Commande.CancelCommande;
using Core.Configuration;
using Core.Events;
using Core.Kafka;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommandeApi.Infrastructure.KafkaBackgroundService;


public class CommandeBackgroundService : BackgroundService
{
    private readonly KafkaConsumer<StockDecrementFailedEvent> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaSettings _settings;
    private readonly ILogger<CommandeBackgroundService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CommandeBackgroundService(
        KafkaConsumer<StockDecrementFailedEvent> consumer,
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaSettings> settings,
        ILogger<CommandeBackgroundService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _consumer = consumer;
        _scopeFactory = scopeFactory; // Nécessaire pour créer des services Scoped (DB, Mediator)
        _settings = settings.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Point d'entrée principal du service lors du démarrage de l'application.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // On lance la consommation asynchrone du topic configuré.
        // On ne met pas de try/catch global ici pour que l'orchestrateur de .NET 
        // puisse détecter si le démarrage du service échoue réellement.
        var consumerTopic = _settings.KafkaTransaction.ConsumerTopic;
        await _consumer.ConsumeAsync(consumerTopic, async (eventMessage) =>
        {
            _logger.LogInformation("{Kafka} : Compensation en cours - Message reçu du topic {Topic} : {Event} traceId : {traceId}", 
                Constante.Prefix.KafkaPrefix,
                consumerTopic, 
                eventMessage,
                _httpContextAccessor?.HttpContext?.TraceIdentifier);

            // IMPORTANT : Chaque message Kafka doit être traité dans son propre Scope.
            // Cela permet d'avoir une instance propre du DbContext et d'ouvrir 
            // une transaction isolée pour chaque message traité.
            using var scope = _scopeFactory.CreateScope();

            // On récupère MediatR à l'intérieur du scope fraîchement créé.
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // On transforme (encapsule) l'événement brut reçu de Kafka 
            // en une Commande métier compréhensible par MediatR.
            var command = new CancelCommandeCommandCompensation(eventMessage);

            // On envoie la commande au bus MediatR.
            // Le PipelineBehavior (Transaction) se déclenchera ici automatiquement.
            // On transmet le 'stoppingToken' pour pouvoir annuler le traitement si l'app s'arrête.
            await mediator.Send(command, stoppingToken);

        }, stoppingToken);
    }

    // Remarque : La méthode Dispose() n'est pas surchargée ici car le 'KafkaConsumer' 
    // est enregistré en tant que Singleton dans l'injection de dépendances (DI).
    // C'est donc le conteneur de services .NET qui s'occupera de le libérer proprement.
}
