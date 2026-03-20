using Core.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ProductApi.Application.Events;


public class CommandeCancelledConsumer : IConsumer<CommandeCancelEvent>
{
    private readonly IUnitOfWorkProduct _unitOfWork;
    private readonly ILogger<CommandeCancelledConsumer> _logger;
    public CommandeCancelledConsumer(IUnitOfWorkProduct unitOfWork, ILogger<CommandeCancelledConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    public async Task Consume(ConsumeContext<CommandeCancelEvent> context)
    {
        var message = context.Message;

        var result = await _unitOfWork.ProductRepository.UpdateStock(message);

        if (result.Count > 0)
        {
            _logger.LogInformation($"MassTransit : Stock updated successfully for CommandeId: {message.CommandeId}");
        }
        else
        {
            _logger.LogError($"MassTransit : Failed to update stock for CommandeId: {message.CommandeId}");
            // Optionnel : tu peux choisir de rejeter le message ou de le mettre en file d'attente pour une réessai ultérieur
            // await context.Publish(new StockUpdateFailedEvent(message.CommandeId));
        }
        // Tu peux aussi republier un autre event si nécessaire
        // await context.Publish(new AnotherEvent(...));
    }
}
