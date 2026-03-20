using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Registry;
using Polly.Retry;
using Polly.Timeout;
using System.Diagnostics;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Forwarder;

namespace Core.Extensions;


// Une factory personnalisée pour créer des HttpClient utilisés par YARP
// Elle permet d'injecter un pipeline de résilience Polly dans le handler
public class PollyForwarderHttpClientFactory : ForwarderHttpClientFactory
{
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly ILogger<PollyForwarderHttpClientFactory> _logger;

    public PollyForwarderHttpClientFactory(
        ResiliencePipelineProvider<string> pipelineProvider,
        ILogger<PollyForwarderHttpClientFactory> logger) // 📝 Injection du logger
    {
        _pipelineProvider = pipelineProvider;
        _logger = logger;
    }

    protected override void ConfigureHandler(ForwarderHttpClientContext context, SocketsHttpHandler handler)
    {
        base.ConfigureHandler(context, handler);
        handler.PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2);
        handler.MaxConnectionsPerServer = 200; // Augmenté pour la haute disponibilité
        handler.EnableMultipleHttp2Connections = true;
    }

    protected override HttpMessageHandler WrapHandler(ForwarderHttpClientContext context, HttpMessageHandler handler)
    {
        var pipeline = _pipelineProvider.GetPipeline<HttpResponseMessage>("yarp-pipeline");

        // On utilise un ResilienceHandler qui loggue les événements
        return new ResilienceHandler(pipeline) { InnerHandler = handler };
    }
}



public static class ResiliencePipelineExtensions
{
    public static IServiceCollection AddYarpResiliencePipeline(this IServiceCollection services)
    {
        // On enregistre un pipeline de résilience nommé "yarp-pipeline"
        // Ce pipeline sera appliqué aux requêtes HTTP envoyées par YARP.
        services.AddResiliencePipeline<string, HttpResponseMessage>("yarp-pipeline", (pipeline, context) =>
        {
            var _logger = context.ServiceProvider.GetRequiredService<ILogger<PollyForwarderHttpClientFactory>>();

            // 🛡️ 1. TIMEOUT GLOBAL (15s)
            // Objectif : éviter qu'une requête reste bloquée trop longtemps.
            // Si le délai est dépassé, on logge l'événement et on annule l'opération.
            pipeline.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(15),
                OnTimeout = args =>
                {
                    _logger.LogError("YARP - TIMEOUT : ⏱️ Timeout global déclenché, TraceId={TraceId}",
                        Activity.Current?.TraceId.ToString() ?? "N/A");
                    return ValueTask.CompletedTask;
                }
            });

            // 🚦 2. RATE LIMITER (Admission Control)
            // Objectif : protéger le système contre une surcharge en limitant le nombre de requêtes par seconde.
            // Ici, on autorise 100 requêtes par seconde avec une file d’attente de 20.
            var fixedWindowLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,              // Nombre max de requêtes par fenêtre
                Window = TimeSpan.FromSeconds(1), // Durée de la fenêtre
                QueueLimit = 20                 // Nombre max de requêtes en attente
            });

            pipeline.AddRateLimiter(new RateLimiterStrategyOptions
            {
                // ✅ Correction : on fournit une fonction d’acquisition explicite.
                // AcquireAsync gère l’asynchronisme et retourne un RateLimitLease.
                RateLimiter = args => fixedWindowLimiter.AcquireAsync(1, args.Context.CancellationToken),

                OnRejected = args =>
                {
                    // Si la limite est atteinte, la requête est rejetée.
                    _logger.LogWarning("YARP - RATELIMITER : 🚦 RateLimiter : requête rejetée (trop de trafic), TraceId={TraceId}",
                        Activity.Current?.TraceId.ToString() ?? "N/A");
                    return ValueTask.CompletedTask;
                }
            });

            // 🔄 3. RETRY INTELLIGENT
            // Objectif : réessayer certaines requêtes en cas d’échec transitoire.
            // On définit une stratégie fine pour éviter de rejouer des opérations non-idempotentes.
            pipeline.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(response =>
                    {
                        // On ne rejoue que les erreurs serveur (HTTP 5xx).
                        bool isServerError = (int)response.StatusCode >= 500;
                        if (!isServerError) return false;

                        // Sécurité : on ne rejoue que si la requête est idempotente
                        // (GET, PUT, DELETE) ou si le client fournit une clé d'idempotence.
                        var method = response.RequestMessage?.Method;
                        bool isSafeMethod = method == HttpMethod.Get || method == HttpMethod.Delete || method == HttpMethod.Put;
                        bool hasIdempotencyKey = response.RequestMessage?.Headers.Contains("X-Idempotency-Key") ?? false;

                        bool canRetry = isSafeMethod || hasIdempotencyKey;

                        if (!canRetry)
                        {
                            // Log explicite pour signaler qu’on ne rejoue pas une requête non-idempotente.
                            _logger.LogWarning("YARP - RETRY 🚦 : Échec {StatusCode} sur {Method}. Aucun retry (non-idempotent). TraceId={TraceId}",
                                response.StatusCode, method, Activity.Current?.TraceId.ToString() ?? "N/A");
                        }
                        return canRetry;
                    })
                    // On rejoue aussi en cas d’exception réseau ou de timeout.
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>(),

                MaxRetryAttempts = 2,              // Nombre max de tentatives
                Delay = TimeSpan.FromMilliseconds(300), // Délai initial
                BackoffType = DelayBackoffType.Exponential, // Backoff exponentiel
                UseJitter = true,                  // Ajout de jitter pour éviter les collisions
                OnRetry = args =>
                {
                    // Log détaillé à chaque retry
                    var uri = args.Outcome.Result?.RequestMessage?.RequestUri?.ToString() ?? "N/A";
                    var status = args.Outcome.Result?.StatusCode.ToString() ?? "Exception";

                    _logger.LogWarning("YARP - ON RETRY 🔄 : Retry #{Attempt} sur {Uri}, Status={Status}, TraceId={TraceId}",
                        args.AttemptNumber + 1,
                        uri,
                        status,
                        Activity.Current?.TraceId.ToString() ?? "N/A");
                    return ValueTask.CompletedTask;
                }
            });

            // 🔌 4. CIRCUIT BREAKER (Dernier rempart)
            // Objectif : éviter d’épuiser les ressources si le service est instable.
            // Le circuit s’ouvre si trop d’échecs sont détectés, puis se referme après un délai.
            pipeline.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,                  // 50% d’échecs déclenchent l’ouverture
                SamplingDuration = TimeSpan.FromSeconds(15), // Fenêtre d’observation
                MinimumThroughput = 10,              // Nombre minimum de requêtes pour calculer le ratio
                BreakDuration = TimeSpan.FromSeconds(30), // Durée d’ouverture du circuit

                OnOpened = args =>
                {
                    _logger.LogError("YARP - CIRCUIT BREAKER (OnOpened) 🚨: CircuitBreaker OUVERT pour {BreakDuration}s. TraceId={TraceId}",
                        args.BreakDuration.TotalSeconds,
                        Activity.Current?.TraceId.ToString() ?? "N/A");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("YARP - CIRCUIT BREAKER (OnClosed) ✅ CircuitBreaker REFERMÉ. TraceId={TraceId}",
                        Activity.Current?.TraceId.ToString() ?? "N/A");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("YARP - CIRCUIT BREAKER (OnHalfOpened) ⚠️ CircuitBreaker en état TEST (Half-Open). TraceId={TraceId}",
                        Activity.Current?.TraceId.ToString() ?? "N/A");
                    return ValueTask.CompletedTask;
                }
            });
        });

        return services;
    }
}
