using Ardalis.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

// Ajoute ce namespace pour accéder à l'environnement
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Core.Models; // Nécessaire pour IDiagnosticContext

namespace Core.Middlewares;

public class GlobalErrorMiddleware
{
    private readonly RequestDelegate _next;                // Poursuit le pipeline HTTP
    private readonly ILogger<GlobalErrorMiddleware> _logger; // Logger standard injecté
    private readonly IHostEnvironment _env;                // Détection Dev/Prod

    public GlobalErrorMiddleware(RequestDelegate next,
                                ILogger<GlobalErrorMiddleware> logger,
                                IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // On laisse la requête traverser les autres middlewares et contrôleurs
            await _next(context);
        }
        catch (Exception ex)
        {
            // Sécurité : Si le flux de réponse a déjà commencé (headers envoyés), 
            // on ne peut plus modifier le StatusCode ou réécrire le Body.
            if (context.Response.HasStarted) throw;

            // --- OPTIMISATION SERILOG ---
            // On récupère manuellement le DiagnosticContext pour lier cette exception 
            // au log de requête généré par app.UseSerilogRequestLogging().
            var diagnosticContext = context.RequestServices.GetService<IDiagnosticContext>();
            if (diagnosticContext != null)
            {
                // Envoie l'exception au logger parent pour qu'il passe en niveau "Error"
                diagnosticContext.SetException(ex);
                // Ajoute une métadonnée pour faciliter le filtrage dans Loki/Seq
                diagnosticContext.Set("ErrorType", ex.GetType().Name);
            }

            // Traduction de l'exception technique en réponse métier sémantique
            var (statusCode, message) = MapException(ex);

            // Détails techniques : Uniquement en mode Développement pour éviter les fuites d'infos en Prod
            string? technicalDetails = null;
            if (_env.IsDevelopment())
            {
                technicalDetails = $"[{ex.GetType().Name}] {ex.Message} | StackTrace: {ex.StackTrace}";
            }

            // Journalisation explicite de l'erreur avec la stack trace complète
            // Note : Grâce à Serilog, ce log partagera le même CorrelationId que le log de requête.
            _logger.LogError(ex, "Exception capturée par le middleware: {Message}", ex.Message);

            // Génération de la réponse JSON finale pour le client
            await HandleExceptionAsync(context, statusCode, message, technicalDetails);
        }
    }

    /// <summary>
    /// Formate et écrit la réponse JSON standardisée dans le flux HTTP.
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, HttpStatusCode statusCode, string message, string? details)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var error = new ValidationError
        {
            Identifier = "TechnicalError",
            ErrorMessage = message
        };

        if (!string.IsNullOrEmpty(details))
        {
            error.ErrorMessage += $" (Détails: {details})";
        }

        var errorResponse = new ApiResponse<object>
        {
            IsSuccess = false,
            IsFailure = true,
            Status = statusCode,
            Errors = new List<ValidationError> { error }
        };

        // Performance : Utilisation de JsonSerializerOptions statiques (évite les allocations répétées)
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, JsonSerializerOptions));
    }

    /// <summary>
    /// Pattern Matching pour mapper les types d'exceptions vers des codes HTTP standards.
    /// </summary>
    private (HttpStatusCode, string) MapException(Exception ex) => ex switch
    {
        // Erreurs de temps d'attente (408)
        TimeoutException or OperationCanceledException
            => (HttpStatusCode.RequestTimeout, "Le délai d'attente a expiré."),

        // Erreurs de format/validation (400)
        ArgumentException or FormatException or System.ComponentModel.DataAnnotations.ValidationException
            => (HttpStatusCode.BadRequest, "Données invalides ou mal formées."),

        // Sécurité (401 & 403)
        System.Security.Authentication.AuthenticationException
            => (HttpStatusCode.Unauthorized, "Authentification requise."),
        UnauthorizedAccessException
            => (HttpStatusCode.Forbidden, "Accès refusé."),

        // Ressources manquantes (404)
        KeyNotFoundException or FileNotFoundException
            => (HttpStatusCode.NotFound, "La ressource demandée est introuvable."),

        // Conflits d'état métier (409)
        InvalidOperationException
            => (HttpStatusCode.Conflict, "L'opération ne peut pas être effectuée dans l'état actuel."),

        // Fonctionnalité absente (501)
        NotImplementedException
            => (HttpStatusCode.NotImplemented, "Cette fonctionnalité n'est pas encore disponible."),

        // Erreurs de base de données (500)
        DbUpdateException
            => (HttpStatusCode.InternalServerError, "Une erreur de persistance est survenue."),

        // Erreur par défaut (500)
        _ => (HttpStatusCode.InternalServerError, "Une erreur technique inattendue est survenue.")
    };

    // Performance : Stocké en statique pour éviter de recréer l'objet d'options à chaque erreur
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}

