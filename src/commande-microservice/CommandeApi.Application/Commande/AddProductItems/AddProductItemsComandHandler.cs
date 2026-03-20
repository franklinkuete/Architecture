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

namespace CommandeApi.Application.Commande.AddProductItems;

public record AddProductItemsCommand(AddProductItemsRequest Request) : ICommand<CommandeResponse>,ICacheInvalidator, IBusinessValidationMarker
{
    public List<string> CacheKeysToInvalidate => new List<string> {
        $"{CommandeConst.ItemCacheKeyPrefix}-{Request.CommandeId}",
        $"{CommandeConst.GetAllCacheKeyPrefix}*" };
};
internal class AddProductItemsComandHandler : ICommandHandler<AddProductItemsCommand, CommandeResponse>
{
    private readonly IUnitOfWorkCommande _unitOfWork;
    private readonly KafkaProducer _producer;
    private readonly KafkaSettings _settings;
    private readonly ILogger<AddProductItemsComandHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AddProductItemsComandHandler(IUnitOfWorkCommande unitOfWork,
        KafkaProducer producer, 
        IOptions<KafkaSettings> settings,
        ILogger<AddProductItemsComandHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _producer = producer;
        _settings = settings.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<CommandeResponse>> Handle(AddProductItemsCommand request, CancellationToken cancellationToken)
    {
        var commandeResult = await _unitOfWork.CommandeRepository.GetCommandeByIdAsync(request.Request.CommandeId!.Value);

        commandeResult!.Statut = StatutCommande.Completed;

        var items = request.Request.ProductItems.Adapt<List<ProductCommande>>();

        foreach (var item in items)
        {
            item.CommandeId = request.Request.CommandeId.Value;
        }

        var result = await _unitOfWork.CommandeRepository
                    .AddItems(items);

        if (result == null)
        {
            return Result.Invalid(new ValidationError("AddItems", $"Une erreur est survenu lors de l'ajout des {items.Count} produit"));
        }

        _logger.LogInformation("{prefix} ✔️ Le(s) {Count} produits ont été ajoutés avec succès à la commande {CommandeId} TraceId : {traceId}",
            Constante.Prefix.HandlerPrefix,
            items.Count, 
            request.Request.CommandeId,
            _httpContextAccessor?.HttpContext?.TraceIdentifier);

       _logger.LogInformation("{prefix} 📨 Début de l'envoi de l'événement Kafka vers le microservice de Produit, pour mettre à jour le stock de produits. TraceId : {traceId}",
           Constante.Prefix.KafkaPrefix,
           _httpContextAccessor?.HttpContext?.TraceIdentifier);

        // 👉 Ici on produit un message Kafka, pour informer le microservice de Produit de mettre à jour le stock des produits ajoutés à la commande.
        // Transformez votre commande en l'événement attendu par le ProductApi
        var eventToSend = new CommandeItemsAddedEvent(AddedProductList:
            request.Request.ProductItems.Select(x => new ProductStock(x.ProductId, x.Qte)).ToList(), request.Request.CommandeId
        );

        // Déterminez le topic Kafka à utiliser (assurez-vous que cela correspond à la configuration de votre ProductApi)
        var producerTopic = _settings.KafkaTransaction.ProducerTopic;

        // Produiction du message Kafka
        await _producer.ProduceAsync(topic: producerTopic!, key: request.Request.CommandeId.ToString()!, message: eventToSend);

        return Result.Success(result.Adapt<CommandeResponse>());

    }
}
