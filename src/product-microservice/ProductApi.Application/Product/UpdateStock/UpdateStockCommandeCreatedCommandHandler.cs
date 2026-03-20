using Ardalis.Result;
using Core.Configuration;
using Core.Events;
using Core.Interfaces;
using Core.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProductApi.Application.Product.UpdateStock;

public record UpdateStockCommandeCreatedCommand(CommandeCreatedEvent Event) : ICommand<ProductResponse>, ICacheInvalidator
{
    public List<string> CacheKeysToInvalidate =>
        Event.products
            .Select(p => $"{ProductConst.ItemCacheKeyPrefix}-{p.ProductId}") // Génère "product-1", "product-2", etc.
            .Append($"{ProductConst.GetAllCacheKeyPrefix}*")                // Ajoute la clé de la liste globale
            .ToList();
}

internal class UpdateStockCommandeCreatedCommandHandler : ICommandHandler<UpdateStockCommandeCreatedCommand, ProductResponse>
{
    private readonly IUnitOfWorkProduct _unitOfWork;
    private readonly KafkaProducer _producer; // ✅ Pour la compensation
    private readonly KafkaSettings _settings;
    private readonly ILogger<UpdateStockCommandeCreatedCommandHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateStockCommandeCreatedCommandHandler(IUnitOfWorkProduct unitOfWork,
        KafkaProducer producer, 
        IOptions<KafkaSettings> settings,
        ILogger<UpdateStockCommandeCreatedCommandHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _producer = producer;
        _settings = settings.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<ProductResponse>> Handle(UpdateStockCommandeCreatedCommand request, CancellationToken cancellationToken)
    {
        
        var errorList = await _unitOfWork.ProductRepository.UpdateStock(request.Event);

        // Si la liste est vide ou si un produit manque (logique métier à affiner selon ton repo)
        if (!errorList.Any())
        {
            _logger.LogInformation("{handlerPrefix} ✔️📨 : Stock mis à jour avec succès pour la commande {CommandeId}, provenant du microservice Commande. (Pas de comensation). TraceId {traceId}", 
                Constante.Prefix.HandlerPrefix,
                request.Event.CommandeId,
                _httpContextAccessor?.HttpContext?.TraceIdentifier);
            return Result.Success();
        }

        // --- DÉBUT DE LA COMPENSATION ---
        // Si on arrive ici, c'est que le stock n'a pas pu être mis à jour
        var errorMessage = string.Join("; ", errorList);
     
        var failureEvent = new StockDecrementFailedEvent(
            request.Event.CommandeId,
            errorMessage,
            request.Event.products
        );

        _logger.LogError("{kafka} ❌ : Échec de la mise à jour du stock provenant de la commande {CommandeId} (microservice), envoi de l'événement de compensation : {event} avec la traceId {trace}", Constante.Prefix.KafkaPrefix, request.Event.CommandeId, failureEvent,_httpContextAccessor?.HttpContext?.TraceIdentifier);

        _logger.LogInformation($"{Constante.Prefix.KafkaPrefix} 📨 Début de la Compensation Kafka : Cause de la compensation : {errorMessage}. TraceId :{_httpContextAccessor?.HttpContext?.TraceIdentifier} Evenement : {failureEvent}");

        // Retour au microservice de Commande : On informe Kafka de l'échec pour que le service Commande puisse compenser (Annuler)
        var producerTopic = _settings.KafkaTransaction.ProducerTopic;
        
        await _producer.ProduceAsync(producerTopic, request.Event.CommandeId!.Value.ToString(), failureEvent);
        // --- FIN DE LA COMPENSATION ---

        return Result.Invalid(new ValidationError("ProductNotFound", "Mise à jour du stock impossible, compensation lancée."));
    }
}
