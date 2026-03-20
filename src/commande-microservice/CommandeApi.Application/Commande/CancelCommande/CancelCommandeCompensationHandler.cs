using Ardalis.Result;
using CommandeApi.Domain.Interfaces;
using CommandeApi.Domain.Models;
using Core.Events;
using Core.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CommandeApi.Application.Commande.CancelCommande;

// Handler de Compensation après ajout d'élément dans une commande, dont le stock est insuffisant
public record CancelCommandeCommandCompensation(StockDecrementFailedEvent Event) : ICommand<CommandeResponse>, ICacheInvalidator
{
    public List<string> CacheKeysToInvalidate => new List<string> {
        $"{CommandeConst.ItemCacheKeyPrefix}-{Event.CommandeId.ToString()}",
        $"{CommandeConst.GetAllCacheKeyPrefix}*" };
}

internal class CancelCommandeCompensationHandler : ICommandHandler<CancelCommandeCommandCompensation, CommandeResponse>
{
    private readonly IUnitOfWorkCommande _unitOfWork;
    private readonly ILogger<CancelCommandeCompensationHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public CancelCommandeCompensationHandler(IUnitOfWorkCommande unitOfWork,
        ILogger<CancelCommandeCompensationHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<Result<CommandeResponse>> Handle(CancelCommandeCommandCompensation request, CancellationToken cancellationToken)
    {
        var reponse = await Cancel(request, cancellationToken);
        return reponse;
    }

    private async Task<Result<CommandeResponse>> Cancel(CancelCommandeCommandCompensation request, CancellationToken cancellationToken)
    {

        var itemsMapped = request.Event.productToRetrieve.Adapt<List<ProductItem>>();

        // Supprimer les items de la commande qui ont échoué à la validation du stock
        // et remettre la commande en statut "Checking Stock" pour permettre une nouvelle tentative de validation du stock ultérieurement
        var updateCommande = await _unitOfWork.CommandeRepository.RestoreStockAfterCompensation(request.Event.CommandeId!.Value, request.Event.productToRetrieve.Adapt<List<ProductItem>>());

        _logger.LogInformation("{Kafka} ✔️📨 : Compensation terminée avec succès - Commande {CommandeId} et ses produits ont été mis à jour avec le statut 'Checking Stock' TraceId {traceId}",
           Constante.Prefix.KafkaPrefix,
            updateCommande!.Id,
            _httpContextAccessor?.HttpContext?.TraceIdentifier);

        // On retourne un succes par principe, même s'il ne sera jamais exploité null part (aucun retour d'api concerné par ce handler)
        return Result.Success(updateCommande!.Adapt<CommandeResponse>());
    }
}

