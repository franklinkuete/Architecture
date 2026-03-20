using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Core.Extensions;

// 📌 Définition d’une méthode d’extension pour IServiceCollection
// Cela permet d’ajouter une configuration OpenTelemetry personnalisée
// directement dans le pipeline de services ASP.NET Core.
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddCustomOpenTelemetry(this IServiceCollection services, string serviceName)
    {
        // 👉 Ici, on va configurer OpenTelemetry (traces, métriques, logs)
        // en utilisant le nom du service passé en paramètre.
        // Le "serviceName" permet d’identifier l’application dans les outils d’observabilité
        // (Grafana Tempo, Prometheus, Seq, etc.).

        services.AddOpenTelemetry()
            .ConfigureResource(r => r
                // 📌 Déclare le service dans OpenTelemetry avec un nom et une version
                .AddService(serviceName, serviceVersion: "1.0.0")

                // 🏷️ Ajout d’attributs globaux pour faciliter le filtrage croisé (ex. Loki/Tempo)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["service.instance.id"] = Guid.NewGuid().ToString() // Identifiant unique de l’instance
                }))

            .WithTracing(t => t
                // 🎯 Sampling (Échantillonnage)
                // On conserve 20% des traces pour limiter la charge réseau tout en gardant assez de données.
                // ParentBasedSampler permet de suivre une trace si le parent l’a déjà initiée.
                .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.2)))

                // 🛡️ Filtrage du bruit : on ignore les appels techniques (health, metrics, swagger, etc.)
                .AddAspNetCoreInstrumentation(o => o.Filter = (req) =>
                {
                    var path = req.Request.Path;

                    return !path.StartsWithSegments("/health")     // Monitoring
                        && !path.StartsWithSegments("/metrics")   // Prometheus
                        && !path.StartsWithSegments("/swagger")   // Interface Swagger
                        && !path.StartsWithSegments("/openapi")   // Doc OpenAPI (.NET 9+)
                        && !path.StartsWithSegments("/favicon.ico") // Bruit navigateur
                        && path != "/";                           // Root (souvent utilisé pour le "Ping")
                })

                // 🔎 Instrumentation des appels HTTP sortants
                .AddHttpClientInstrumentation()

                // 🗄️ Instrumentation des requêtes EF Core (base de données)
                .AddEntityFrameworkCoreInstrumentation()

                // 🚀 Export des traces vers deux backends (expérimental : car normalement on a juste besoin d'un seul AddOtlpExporter)
                // tempo (grafana) suffit largement, mais je voulais tester aussi Seq pour voir la différence
                // (tempo est plus orienté traces, seq est plus orienté logs mais supporte aussi les traces)
                .AddOtlpExporter(o => o.Endpoint = new Uri("http://tempo:4316")) // Vers Grafana Tempo
                .AddOtlpExporter(o => o.Endpoint = new Uri("http://seq:80/ingest/otlp/v1/traces"))) // Vers Seq

            .WithMetrics(m => m
                // 📊 Instrumentation des métriques ASP.NET Core (requêtes HTTP, etc.)
                .AddAspNetCoreInstrumentation()

                // ⚙️ Instrumentation runtime (.NET) : CPU, RAM, GC
                .AddRuntimeInstrumentation()

                // 🔎 Instrumentation des appels HTTP sortants
                .AddHttpClientInstrumentation()

                // 🚀 Export des métriques vers Prometheus et Seq (expérimental aussi)
                // Prometheus est idéal pour les métriques, Seq peut aussi les recevoir mais c’est moins courant (plus orienté logs)
                .AddPrometheusExporter() // Exposé sur /metrics pour Prometheus/Grafana
                .AddOtlpExporter(o => o.Endpoint = new Uri("http://seq:80/ingest/otlp/v1/metrics"))); // Vers Seq

        return services;
    }
}