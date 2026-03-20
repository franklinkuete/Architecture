using Ardalis.Result;
using CommandeApi.Domain.Interfaces;
using CommandeApi.Domain.Models;
using Core.Events;
using Core.Interfaces;
using Mapster;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CommandeApi.Application.Commande.CancelCommande;

// Test de MassTransit pour la production d'événement de commande annulé, qui sera consommé par le ProductApi pour remettre le stock à jour
public record CancelCommande(int? CommandeId) : ICommand<CommandeResponse>, ICacheInvalidator
{
    public List<string> CacheKeysToInvalidate => new List<string> { $"{CommandeConst.ItemCacheKeyPrefix}-{CommandeId.ToString()}", $"{CommandeConst.GetAllCacheKeyPrefix}*" };
}

internal class CancelCommandeHandler : ICommandHandler<CancelCommande, CommandeResponse>
{
    private readonly IUnitOfWorkCommande _unitOfWork;
    private readonly ITopicProducer<CommandeCancelEvent> _massTransitTopicProducer;
    private readonly ILogger<CancelCommandeHandler> _logger;

    public CancelCommandeHandler(IUnitOfWorkCommande unitOfWork, ITopicProducer<CommandeCancelEvent> massTransitTopicProducer,
        ILogger<CancelCommandeHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _massTransitTopicProducer = massTransitTopicProducer;
        _logger = logger;
    }
    public async Task<Result<CommandeResponse>> Handle(CancelCommande request, CancellationToken cancellationToken)
    {
        var commande = await _unitOfWork.CommandeRepository.GetCommandeByIdAsync(request.CommandeId!.Value);

        commande!.Statut = StatutCommande.Cancel;
        var updateCommande = await _unitOfWork.CommandeRepository.UpdateCommandeAsync(request.CommandeId!.Value, commande);

        if (updateCommande != null)
        {
            var products = updateCommande!.ProductItems
                         .Select(x => new ProductStock(x.ProduitId, x.Quantite))
                         .ToList();

            var commandeCanceledEvent = new CommandeCancelEvent(products, request.CommandeId);

            await _massTransitTopicProducer.Produce(commandeCanceledEvent, cancellationToken);
           
            _logger.LogInformation($"Commande with id {request.CommandeId} has been canceled and CommandeCancelEvent:{commandeCanceledEvent} has been published by MassTransit.");    
            return Result.Success(updateCommande!.Adapt<CommandeResponse>());
        }

        return Result.Invalid(new ValidationError("CancelFailed", "Impossible de mettre à jour cette command"));
    }
}

