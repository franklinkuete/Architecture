using Ardalis.Result;
using CommandeApi.Domain.Interfaces;
using CommandeApi.Domain.Models;
using Core.Configuration;
using Core.Events;
using Core.Interfaces;
using Core.Kafka;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace CommandeApi.Application.Commande.AddCommande;

public record AddCommandeCommand(CommandeRequest commande) : ICommand<CommandeResponse>, ICacheInvalidator,          // Marqueur pour déclencher l'invalidation automatique du cache (via un Pipeline Behavior)
      IBusinessValidationMarker
{
    public List<string> CacheKeysToInvalidate => new List<string> { $"{CommandeConst.GetAllCacheKeyPrefix}*" };
}

internal sealed class AddCommandeHandler : ICommandHandler<AddCommandeCommand, CommandeResponse>
{
    private readonly IUnitOfWorkCommande _unitOfWork;
    private readonly KafkaProducer _producer;
    private readonly KafkaSettings _settings;
    private readonly ILogger<AddCommandeHandler> _logger;  
    private readonly IHttpContextAccessor? _httpContextAccessor; 


    public AddCommandeHandler(IUnitOfWorkCommande unitOfWork, KafkaProducer producer,
        IOptions<KafkaSettings> settings, ILogger<AddCommandeHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _producer = producer;
        _settings = settings.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<Result<CommandeResponse>> Handle(AddCommandeCommand request, CancellationToken cancellationToken)
    {
        var newCommande = request.commande.Adapt<CommandeApi.Domain.Models.Commande>();
        newCommande.Statut = StatutCommande.Completed;
        newCommande.Date = DateTime.UtcNow;

        var entity = await _unitOfWork.CommandeRepository
            .AddCommandeAsync(newCommande);

        if (entity == null)
        {
            // Cas métier : pas de commande créée
            Result.Invalid(new ValidationError("NewCommandeError", "La commande n'a pas pu être créée"));
        }
        _logger.LogInformation("{prefix} ✔️ Le(s) {Count} produits ont été ajoutés avec succès à la commande {CommandeId} TraceId : {traceId}",
            Constante.Prefix.HandlerPrefix,
            newCommande.ProductItems.Count,
            entity!.Id,
            _httpContextAccessor?.HttpContext?.TraceIdentifier);

        _logger.LogInformation("{prefix} 📨 Début de l'envoi de l'événement Kafka vers le microservice de Produit, pour mettre à jour le stock de produits. TraceId : {traceId}",
            Constante.Prefix.KafkaPrefix,
            _httpContextAccessor?.HttpContext?.TraceIdentifier);
        var newOrder = entity.Adapt<CommandeResponse>();
        
        // 👉 Ici on produit un message Kafka
        // Transformez votre commande en l'événement attendu par le ProductApi
        var eventToSend = new CommandeCreatedEvent(products:
            newOrder!.ProductItems.Select(x => new ProductStock(x.ProductId, x.Qte)).ToList(), newOrder.Id
        );
        var producerTopic = _settings.KafkaTransaction.ProducerTopic;
        await _producer.ProduceAsync(topic: producerTopic!, key: newOrder!.Id.ToString(), message: eventToSend);

        return Result.Success(newOrder!);
    }
}
