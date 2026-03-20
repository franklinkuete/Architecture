using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Core.Mediatr.Behavior;
/// <summary>
/// Pipeline Behavior MediatR qui mesure le temps d'exécution d'une requête.
/// Permet de logguer en Information, Warning ou Error selon des seuils configurables.
/// </summary>
/// <typeparam name="TRequest">Type de la requête MediatR (non nullable).</typeparam>
/// <typeparam name="TResponse">Type de la réponse MediatR.</typeparam>
public class MetricsPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<MetricsPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly int _warningThresholdMs;
    private readonly int _errorThresholdMs;
    private readonly IHttpContextAccessor _httpContextAccessor;


    /// <summary>
    /// Constructeur du pipeline.
    /// Les seuils sont récupérés depuis la configuration (appsettings.json ou variables d'environnement).
    /// </summary>
    public MetricsPipelineBehavior(
    ILogger<MetricsPipelineBehavior<TRequest, TResponse>> logger,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;

        // ⚙️ Seuils configurables : par défaut Warning = 500ms, Error = 2000ms
        // Ici, on lit les valeurs depuis la configuration (appsettings.json ou autre).
        // Si elles ne sont pas définies, on utilise les valeurs par défaut : Warning = 300ms, Error = 1200ms.
        _warningThresholdMs = configuration.GetValue<int>("Metrics:WarningThresholdMs", 300);
        _errorThresholdMs = configuration.GetValue<int>("Metrics:ErrorThresholdMs", 1200);
    }

    /// <summary>
    /// Méthode exécutée pour chaque requête MediatR.
    /// Elle mesure le temps d'exécution et génère des logs en fonction des seuils configurés.
    /// </summary>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        // ⏳ Exécution de la requête suivante dans le pipeline
        var response = await next();

        sw.Stop();
        var elapsed = sw.ElapsedMilliseconds;

        // Si la réponse est une collection, on peut loguer le nombre d’éléments retournés
        int? itemCount = null;
        if (response is Ardalis.Result.IResult resultat && resultat.GetValue() is IEnumerable<object> list)
        {
            itemCount = list.Count();
        }

        // 🔎 Détermination du niveau de log selon les seuils
        // - Error si temps > seuil d’erreur
        // - Warning si temps > seuil d’avertissement
        // - Information sinon
        var logLevel = elapsed > _errorThresholdMs
            ? LogLevel.Error
            : elapsed > _warningThresholdMs
                ? LogLevel.Warning
                : LogLevel.Information;

        // On tente d'abord de récupérer l'identifiant de trace distribué
        // fourni par System.Diagnostics.Activity (souvent propagé via OpenTelemetry).
        // C'est l'ID recommandé pour la corrélation inter-services.
        var traceId = Activity.Current?.TraceId.ToString()

            // Si aucun Activity n'est en cours (par exemple, contexte non instrumenté),
            // on se rabat sur l'identifiant de requête généré par ASP.NET Core.
            // Celui-ci est unique par requête HTTP, mais reste local au serveur.
            ?? _httpContextAccessor.HttpContext?.TraceIdentifier

            // Enfin, si on n'est pas dans un contexte HTTP (ex. tâche en arrière-plan),
            // on utilise une valeur par défaut pour signaler qu'il s'agit d'un traitement hors requête.
            ?? "Background-Task";

        // 📊 Construction du message de log
        if (itemCount.HasValue)
        {
            _logger.Log(
                logLevel,
                "{@prefix} ⏱ Requête {Request} exécutée en {Elapsed} ms (seuils: erreur={ErrorThreshold}, avertissement={WarningThreshold}) : Nombre d’éléments retournés {Count}, CorrelationId {TraceId}",
                Constante.Prefix.MetricsPrefix,
                typeof(TRequest).Name,
                elapsed,
                _errorThresholdMs,
                _warningThresholdMs,
                itemCount.Value,
               traceId
            );
        }
        else
        {
            _logger.Log(
                logLevel,
                "{@prefix} ⏱ Requête {Request} exécutée en {Elapsed} ms (seuils: erreur={ErrorThreshold}, avertissement={WarningThreshold}) : CorrelationId {TraceId}",
                Constante.Prefix.MetricsPrefix,
                typeof(TRequest).Name,
                elapsed,
                _errorThresholdMs,
                _warningThresholdMs,
                traceId
            );
        }

        return response;
    }
}