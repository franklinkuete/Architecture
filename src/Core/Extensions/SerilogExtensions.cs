using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Grafana.Loki;
using System.Text;

namespace Core.Extensions;

public static class SerilogExtensions
{
    /// <summary>
    /// Configure Serilog au démarrage (WebApplicationBuilder).
    /// Performance : Utilise des Overrides et des Sinks asynchrones.
    /// </summary>
    public static void ConfigureSerilog(this WebApplicationBuilder builder, string serviceName)
    {
        var env = builder.Environment.EnvironmentName;

        // UseSerilog injecte Serilog comme fournisseur de log unique et gère proprement 
        // le cycle de vie (évite les fuites de mémoire liées au static Log.Logger).
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                // 1. Charge les paramètres du appsettings.json (priorité aux fichiers de config)
                .ReadFrom.Configuration(context.Configuration)

                // 2. Niveaux de log : On ignore le bruit de fond de .NET pour ne pas saturer le réseau/disque
                .MinimumLevel.Information()
                .MinimumLevel.Override("Yarp", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Server.Kestrel", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Infrastructure", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.ModelBinding", LogEventLevel.Warning)

                // Performance : L'override est plus rapide qu'un filtre .Filter.ByExcluding(...)
                .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker", LogEventLevel.Warning)

                // 3. Enrichisseurs : Ajout de métadonnées cruciales pour le traçage
                .Enrich.FromLogContext() // Permet d'utiliser LogContext.PushProperty
                .Enrich.WithCorrelationIdHeader("x-correlation-id") // Traçabilité distribuée
                .Enrich.WithProperty("Environment", env)
                .Enrich.WithProperty("ApplicationService", serviceName)
                .Enrich.WithExceptionDetails() // Formate les exceptions avec toutes leurs InnerExceptions
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()

                // 4. Destinations (Sinks)
                .WriteTo.Console()
                // Performance : Toujours utiliser .Async pour les destinations réseau (Seq, Loki)
                // pour ne pas bloquer le thread principal en cas de latence réseau.
                .WriteTo.Async(a => a.Seq("http://seq:80"))
                .WriteTo.Async(a => a.GrafanaLoki("http://loki:3100",
                    labels: new[]
                    {
                        new LokiLabel { Key = "service", Value = serviceName },
                        new LokiLabel { Key = "environment", Value = env }
                    },
                    // Robustesse : On n'indexe QUE l'environnement. 
                    // On ne met PAS le CorrelationId ici (trop de valeurs uniques = crash de Loki).
                    propertiesAsLabels: new[] { "environment" }
                ));
        });
    }

    /// <summary>
    /// Middleware personnalisé pour logger les requêtes HTTP de façon optimisée.
    /// </summary>
    public static IApplicationBuilder UseCustomSerilogLogging(this IApplicationBuilder app, string serviceName)
    {
        // Liste des clés sensibles à masquer(en minuscules pour la comparaison)
        // Tu peux ajouter "token", "password", "secret", "apikey", etc.
        string[] sensitiveKeys = ["token", "password", "secret", "apikey", "email", "authorization"];

        // Remplace le logging par défaut d'ASP.NET (trop verbeux) par un log unique et structuré par requête.
        app.UseSerilogRequestLogging(options =>
        {
            // Performance : DiagnosticContext regroupe toutes les propriétés pour les écrire d'un coup.
            // 🎯 Enrichissement du contexte de diagnostic pour chaque requête HTTP terminée
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                var query = httpContext.Request.Query;
                string filteredQuery = string.Empty;

                // On ne traite la QueryString que si elle contient des paramètres (évite les calculs inutiles)
                if (query.Count > 0)
                {
                    // Performance : Utilisation de StringBuilder pour construire la chaîne de caractères
                    // sans saturer la mémoire (évite les allocations répétées de 'string')
                    var queryStringBuilder = new StringBuilder("?");
                    bool first = true;

                    foreach (var item in query)
                    {
                        // Ajoute le séparateur '&' entre les paramètres (sauf pour le premier)
                        if (!first) queryStringBuilder.Append('&');

                        queryStringBuilder.Append(item.Key);
                        queryStringBuilder.Append('=');

                        // 🛡️ SÉCURITÉ : Vérifie si la clé fait partie de la liste 'sensitiveKeys' définie plus haut
                        // On compare en minuscules (.ToLower) pour ignorer la casse (ex: 'Token' ou 'token')
                        if (sensitiveKeys.Contains(item.Key.ToLower()))
                        {
                            // On remplace la valeur sensible par des étoiles pour ne jamais la stocker dans les logs
                            queryStringBuilder.Append("***");
                        }
                        else
                        {
                            // On garde la valeur réelle pour les paramètres non sensibles (ex: ?page=1)
                            queryStringBuilder.Append(item.Value);
                        }

                        first = false;
                    }
                    filteredQuery = queryStringBuilder.ToString();
                }

                // Ajoute les propriétés aux métadonnées structurées du log final.
                // {QueryString} sera injecté dans le MessageTemplate de Serilog.
                diagnosticContext.Set("QueryString", filteredQuery);
                diagnosticContext.Set("ServiceName", serviceName);
            };

            // Template lisible et structuré pour la recherche rapide
            options.MessageTemplate = "🌐 {ServiceName} → {RequestMethod} {RequestPath}{QueryString} → {StatusCode} in {Elapsed:0.000} ms";

            // Logique intelligente pour déterminer le niveau de log
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null) return LogEventLevel.Error; // Exception = Erreur
                if (elapsed > 1000) return LogEventLevel.Warning; // Lenteur (>1s) = Warning

                // Filtrage du bruit (Healthchecks, Metrics, Favicon) en Verbose (souvent ignoré)
                var path = httpContext.Request.Path;

                if (path.StartsWithSegments("/metrics") ||
                    path.StartsWithSegments("/health") ||
                    path.StartsWithSegments("/favicon.ico") ||
                    path.StartsWithSegments("/swagger") || // Bruit Swagger UI
                    path.StartsWithSegments("/openapi") || // Bruit API Doc .NET 9
                    path == "/")                           // Bruit Root/Ping
                {
                    return LogEventLevel.Verbose;
                }

                return LogEventLevel.Information;
            };
        });

        return app;
    }
}


