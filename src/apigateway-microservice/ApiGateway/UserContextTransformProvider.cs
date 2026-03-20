namespace ApiGateway;

using Core.Interfaces;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

/// <summary>
/// TransformProvider personnalisé pour injecter le contexte utilisateur
/// dans les requêtes proxifiées par YARP (ApiGateway).
/// </summary>
public class UserContextTransformProvider : ITransformProvider
{
    // ⚠️ On peut injecter des services ici via le constructeur,
    // mais uniquement des Singletons (les Scoped doivent être récupérés
    // dans le HttpContext.RequestServices au moment de la requête).

    /// <summary>
    /// Validation des routes configurées dans YARP.
    /// Ici, aucune règle spécifique n’est imposée.
    /// </summary>
    public void ValidateRoute(TransformRouteValidationContext context)
    {
        /* Pas de validation spécifique */
    }

    /// <summary>
    /// Validation des clusters (groupes de destinations).
    /// Ici, aucune règle spécifique n’est imposée.
    /// </summary>
    public void ValidateCluster(TransformClusterValidationContext context)
    {
        /* Pas de validation spécifique */
    }

    /// <summary>
    /// Application des transformations globales à toutes les routes.
    /// C’est ici qu’on définit les comportements communs et qu’on ajoute
    /// notre logique personnalisée.
    /// il transfer les informations du contexte utilisateur (UserContext) dans les en-têtes HTTP
    /// </summary>
    public void Apply(TransformBuilderContext context)
    {
        // ✅ Copie des headers de la requête entrante vers la requête proxifiée
        context.CopyRequestHeaders = true;

        // ✅ Suppression des en-têtes X-Forwarded (on les reconstruit proprement)
        context.AddXForwarded(ForwardedTransformActions.Remove);

        // 🔧 Ajout d’une transformation personnalisée
        context.AddRequestTransform(async transformContext =>
        {
            // Récupération du service Scoped IUserContext
            // (lié à la requête en cours, injecté via DI).
            var userContext = transformContext.HttpContext.RequestServices
                .GetRequiredService<IUserContext>();

            // Vérification si l’utilisateur est authentifié
            if (userContext.IsAuthenticated)
            {
                var headers = transformContext.ProxyRequest.Headers;

                // 📌 Transfert des informations essentielles du contexte utilisateur
                headers.Add("X-User-Id", userContext.UserId);
                headers.Add("X-User-Email", userContext.Email);
                headers.Add("X-User-Roles", string.Join(",", userContext.Roles));
                headers.Add("X-User-Claims", string.Join(",", userContext.Claims));
                headers.Add("X-User-IpAddress", userContext.IpAddress);
                headers.Add("X-User-Culture", userContext.Culture);

                // 📌 Ajout du TenantId si disponible (multi-tenant)
                if (!string.IsNullOrEmpty(userContext.TenantId))
                    headers.Add("X-Tenant-Id", userContext.TenantId);

                // 📌 Ajout du CorrelationId pour le tracing distribué
                headers.Add("X-Correlation-Id", userContext.CorrelationId);
            }
        });
    }
}
