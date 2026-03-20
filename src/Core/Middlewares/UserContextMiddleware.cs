using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
/// <summary>
/// Middleware responsable d'extraire les informations de l'utilisateur connecté 
/// et de remplir le service IUserContext pour le reste du cycle de vie de la requête.
/// </summary>
public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Injected IUserContext est généralement "Scoped" pour être unique par requête.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IUserContext userContext)
    {
        // Récupération du ClaimsPrincipal (l'utilisateur identifié par ASP.NET Core)
        var principal = context.User;

        // VÉRIFICATION : L'utilisateur est-il authentifié via JWT ou Cookie ?
        if (principal?.Identity?.IsAuthenticated == true)
        {
            // 1. IDENTITÉ DE BASE
            userContext.IsAuthenticated = true;
            // NameIdentifier correspond souvent à l'ID (sub) dans le jeton
            userContext.UserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            userContext.UserName = principal.Identity?.Name ?? string.Empty;
            userContext.Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

            // Extraction de tous les rôles pour la gestion des permissions
            userContext.Roles = principal.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            // Stockage de tous les claims bruts dans un dictionnaire pour accès rapide
            userContext.Claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value);

            // 2. MÉTADONNÉES MÉTIER (Multi-tenancy / Localisation)
            // Idéal pour filtrer les données par client (TenantId) de manière automatique
            userContext.TenantId = principal.FindFirst(CustomClaimTypes.TenantId)?.Value;

            // Gestion de la langue (Culture) : priorité au Claim, sinon culture du serveur
            userContext.Culture = principal.FindFirst("Culture")?.Value
                                  ?? System.Globalization.CultureInfo.CurrentCulture.Name;

            userContext.TimeZone = principal.FindFirst("Timezone")?.Value;

            // 3. CONTEXTE TECHNIQUE (Audit / Logging)
            userContext.IpAddress = context.Connection.RemoteIpAddress?.ToString();
            userContext.UserAgent = context.Request.Headers["User-Agent"].ToString();
            // Le TraceIdentifier permet de lier les logs à une requête spécifique (Correlation)
            userContext.CorrelationId = context?.TraceIdentifier;

            // 4. RACCOURCIS DE CONTRÔLE
            // Permet de vérifier rapidement si l'utilisateur est un super-admin
            userContext.IsAdmin = string.Equals(principal.FindFirst("IsAdmin")?.Value, "True", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // Utilisateur anonyme
            userContext.IsAuthenticated = false;
        }

        // Passage au middleware suivant (ou au contrôleur)
        await _next(context!);
    }

    /// <summary>
    /// Centralisation des noms de Claims personnalisés pour éviter les "Magic Strings".
    /// </summary>
    public static class CustomClaimTypes
    {
        public const string TenantId = "TenantId";
    }
}
