using Ardalis.Result;
using Core.Helpers;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Core.Mediatr.Behavior;

public class RequestValidatorPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Ardalis.Result.IResult
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<RequestValidatorPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestValidatorPipelineBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<RequestValidatorPipelineBehavior<TRequest, TResponse>> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _validators = validators;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
        var requestName = typeof(TRequest).Name;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
            ["RequestName"] = requestName
        }))
        {
            // 1. Sortie immédiate si rien à valider
            if (!_validators.Any())
            {
                _logger.LogInformation("{@prefix} ℹ️ Aucun validateur trouvé pour {RequestName}, passage direct au handler (TraceId: {TraceId})",
                    Constante.Prefix.RequestValidationPrefix, requestName, traceId);

                return await next();
            }

            var context = new ValidationContext<TRequest>(request);

            // 2. Exécution : Task.WhenAll est bien si asynchrone,
            // mais pour l'allocation, on peut traiter les résultats plus finement.
            _logger.LogInformation("{@prefix} 🔍 Validation en cours pour {RequestName} (TraceId: {TraceId})",
                Constante.Prefix.RequestValidationPrefix, requestName, traceId);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            // 3. Extraction efficace (évite les SelectMany complexes)
            var failures = validationResults
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .ToArray(); // ToArray est souvent plus léger que ToList pour le stockage temporaire

            if (failures.Length != 0)
            {
                _logger.LogWarning("{@prefix} ❌ Validation échouée pour {RequestName}. Erreurs: {Errors} (TraceId: {TraceId})",
                    Constante.Prefix.RequestValidationPrefix, requestName,
                    string.Join(", ", failures.Select(f => f.ErrorMessage)), traceId);

                // 4. Transformation directe vers ton type final
                var validationErrors = new List<ValidationError>(failures.Length);
                foreach (var f in failures)
                {
                    validationErrors.Add(new ValidationError(f.PropertyName, f.ErrorMessage, f.ErrorCode, ValidationSeverity.Error));
                }

                // 5. Instanciation via ton helper performant
                return ResultFactory<TResponse>.Invalid(validationErrors);
            }

            _logger.LogInformation("{@prefix} ✔️ Validation de requête réussie pour {RequestName}, passage au handler (TraceId: {TraceId})",
                Constante.Prefix.RequestValidationPrefix, requestName, traceId);

            return await next();
        }
    }
}
