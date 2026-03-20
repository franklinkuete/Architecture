using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Core.Behavior;


public class LoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Ardalis.Result.IResult
{
    private readonly ILogger<LoggingPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoggingPipelineBehavior(
        ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();

        // On crée un scope de log enrichi avec le TraceId et le nom de la requête
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
            ["RequestName"] = requestName
        }))
        {
            // Log de début d’exécution
            _logger.LogInformation("{@prefix}🚀 Requête {RequestName} démarrée (TraceId: {TraceId})",
                Constante.Prefix.HandlerPrefix, requestName, traceId);

            var response = await next();

            if (!response.IsOk())
            {
                // En cas d’échec, on log uniquement les messages d’erreurs
                var errorMessages = string.Join(", ", response.ValidationErrors.Select(e => e.ErrorMessage));

                _logger.LogError("{@prefix} ❌ Requête {RequestName} échouée. Erreurs: {Errors} (TraceId: {TraceId})",
                    Constante.Prefix.HandlerPrefix, requestName, errorMessages, traceId);
            }
            else
            {
                // Si la réponse contient une collection, on log le nombre d’éléments
                var value = response.GetValue();
                if (value is IEnumerable<object> collection)
                {
                    var count = collection.Count();
                    _logger.LogInformation("{@prefix} ✔️ Requête {RequestName} terminée (Éléments: {Count}, TraceId: {TraceId})",
                        Constante.Prefix.HandlerPrefix, requestName, count, traceId);
                }
                else
                {
                    // Sinon, on log simplement la complétion
                    _logger.LogInformation("{@prefix} ✔️ Requête {RequestName} terminée (TraceId: {TraceId})",
                        Constante.Prefix.HandlerPrefix, requestName, traceId);
                }
            }

            return response;
        }
    }
}