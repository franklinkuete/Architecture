# 🚀 E-Commerce Microservices Ecosystem (.NET 10)

Je m'appelle **Franklin KUETE** (Architecte Solutions), et je vous présente mon écosystème **Cloud-Native** ultra-performant.  
Ce projet est une démonstration d'architecture distribuée moderne, mettant l'accent sur la **résilience**, l'**observabilité** et la **séparation stricte des domaines métier**, le tout articulé autour d'une **Clean Architecture**.

---

## 📖 1. Description du Projet
J'ai conçu cette plateforme pour gérer le cycle de vie complet d'un système e-commerce (**Clients, Produits, Commandes**).  
Elle est bâtie pour supporter une charge importante grâce à :
- ⚡ Une stratégie de **caching hybride** (L1/L2).
- 📡 Une **communication asynchrone** haute performance.
- 🛡️ Une **isolation totale** des données par service.

---

## 🛠 2. Stack Technologique & Écosystème


| Composant | Technologie / Approche |
| :--- | :--- |
| **Framework** | 🚀 .NET 10 (ASP.NET Core API) |
| **API Gateway** | 🚪 YARP (Reverse Proxy) avec Polly Resilience |
| **Bases de Données** | 🗄️ SQL Server 2025, PostgreSQL 16, MySQL 8.3, MariaDB 10.11 |
| **Messaging** | 📡 Apache Kafka & MassTransit |
| **Caching** | ⚡ Redis 7.2 (Distribué L2) + IMemoryCache (Local L1) |
| **Patterns** | ⚔️ CQRS (MediatR) + 🧩 Clean Architecture |
| **Style** | 🌐 Microservices & 📡 Event-Driven Architecture (EDA) |
| **Conteneurisation** | 🐳 Docker-Compose (Orchestration & Isolation) |
| **Observabilité** | 🔦 OpenTelemetry, Tempo, Grafana, Prometheus, Loki & Seq |

---

## 🧬 3. Styles & Patterns d’Architecture
Le projet implémente les standards les plus rigoureux de l'industrie :

- **Clean Architecture** : Découplage total entre Domain, Application, Infrastructure et UI.
- **CQRS** : Séparation stricte des lectures (Queries) et écritures (Commands) via MediatR.
- **Database-per-Service** : Chaque microservice possède sa propre instance de base de données pour une autonomie totale.
- **Unit of Work & Repository** : Abstraction de la couche de données pour des transactions atomiques.

---

## 📦 4. Présentation des Microservices


| Service | Base de Données | Rôle Principal |
| :--- | :--- | :--- |
| **ApiGateway** | SQL Server | YARP, Auth JWT (Identity), Routage, Sécurité & Users. |
| **ClientApi** | PostgreSQL | Gestion du référentiel client et profils. |
| **ProductApi** | MySQL | Catalogue produits et synchronisation des stocks. |
| **CommandeApi** | MariaDB | Orchestration des commandes et émission d'événements. |

---


## ⛓️ 5. Pipeline de Traitement Unifié (MediatR)
Chaque requête traverse une chaîne de Behaviors :

1. 🕒 Metrics : Mesure de la latence globale  
2. 📝 Logging : Traçabilité via TraceId (Serilog)  
3. ✅ Validation Request : Rejet immédiat si le format est invalide (FluentValidation)  
4. 🗂️ Cache Check : Retour immédiat si la donnée est présente en L1/L2  
5. 🔐 Transaction : Ouverture du scope SQL pour les Commands  
6. ⚖️ Business Validation : Vérification des règles métier complexes en base  
7. 🧹 Cache Invalidation : Nettoyage automatique des clés liées en cas de modification  


L'ordre d'enregistrement des pipelines est crucial. Voici l'enchaînement configuré dans l'application :

```csharp
// Enregistrement des pipelines behavior MediatR

// 0. Metrics : mesure le temps global d'exécution de la requête
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MetricsPipelineBehavior<,>));

// 1. Logging : log systématique de l'entrée et de la sortie de la requête
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));

// 2. Validation : vérifie les contraintes de format (FluentValidation) avant toute transaction
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RequestValidatorPipelineBehavior<,>));

// 3. Cache : retourne la réponse immédiatement si elle est déjà stockée (requêtes de lecture)
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CachePipelineBehavior<,>));

// 4. Transaction : ouvre la transaction uniquement pour les Commands valides
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>));

// 5. Business Validation : validations métier complexes avant le traitement final
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(BusinessValidationPipelineBehavior<,>));

// 6. Cache Invalidation : nettoie le cache suite à une modification (CUD)
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CacheInvalidationPipelineBehavior<,>));
```


## ⚡ 6. Stratégie de Caching Hybride
Le **HybridCacheService** résout le problème de latence réseau :

- **L1 (Local)** : Stocké en RAM (Vitesse éclair 🚀)  
- **L2 (Redis)** : Partagé entre les instances (Cohérence 🤝)  
- **Pub/Sub Invalidation** : Lorsqu'une donnée change, Redis notifie toutes les instances pour vider leur cache L1 local instantanément  
```csharp
public class CachePipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
{
    private readonly IHybridCacheService _cache;
    private readonly ILogger<CachePipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Optimisation : on extrait le type de la valeur contenue dans le Result (ex: UserDto)
    private static readonly Type _valueType = typeof(TResponse).GetGenericArguments()[0];

    public CachePipelineBehavior(
        IHybridCacheService cache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CachePipelineBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
        var requestName = typeof(TRequest).Name;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
            ["RequestName"] = requestName,
            ["CacheKey"] = request.CacheKey
        }))
        {
            _logger.LogInformation("{@prefix} 🔍 Tentative de récupération depuis le cache L1 (Mémoire) ou L2 (Redis via HybridCache) (Key: {CacheKey}, TraceId: {TraceId})",
                Constante.Prefix.CachePrefix, request.CacheKey, traceId);

            // 1. Tentative de récupération (L1 : mémoire locale / L2 : Redis via HybridCache)
            var cachedData = await _cache.GetAsync(request.CacheKey, _valueType, request.Policy.MemoryTtl);

            if (cachedData is not null)
            {
                _logger.LogInformation("{@prefix} ✅ Cache HIT pour {RequestName} (Key: {CacheKey}, TraceId: {TraceId})",
                    Constante.Prefix.CachePrefix, requestName, request.CacheKey, traceId);

                return ResultFactory<TResponse>.Success(cachedData);
            }

            _logger.LogInformation("{@prefix} ⚠️ Cache MISS pour {RequestName} (Key: {CacheKey}, TraceId: {TraceId})",
                Constante.Prefix.CachePrefix, requestName, request.CacheKey, traceId);

            // 2. Exécution réelle de la requête
            var response = await next();

            // 3. Mise en cache uniquement si succès métier
            if (response is Ardalis.Result.IResult result && result.IsOk())
            {
                // Reccupération de la valeur contenue dans le Result (ex: ProductResponse)
                var value = result.GetValue();

                if (value is IEnumerable list && !list.GetEnumerator().MoveNext())
                {
                    _logger.LogInformation("{@prefix} ℹ️ Résultat vide, pas de mise en cache (Key: {CacheKey}, TraceId: {TraceId})",
                        Constante.Prefix.CachePrefix, request.CacheKey, traceId);

                    return response;
                }

                await _cache.SetAsync(request.CacheKey, value, request.Policy, ct);

                _logger.LogInformation("{@prefix} 💾 Valeur mise en cache pour {RequestName} (Key: {CacheKey}, TTL: {Ttl}, TraceId: {TraceId})",
                    Constante.Prefix.CachePrefix, requestName, request.CacheKey, request.Policy.MemoryTtl, traceId);
            }

            return response;
        }
    }
}
```

---

## 🛡️ 7. Sécurité & Validation
- **Gateway Security** : Centralisation de l'Identity et injection du UserContext dans les headers  
```csharp
// 🚀 Configuration de Kestrel
builder.WebHost.ConfigureKestrel(options => {
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 1000;
    // Optionnel : Désactive les limites de taille de corps de requête si besoin
    options.Limits.MaxRequestBodySize = null;
});

// 🔖 Récupère le nom du service (assemblage) pour l’utiliser dans la configuration
var serviceName = typeof(Program).Assembly.GetName().Name!;

// 📝 Configure Serilog pour la journalisation centralisée et structurée
builder.ConfigureSerilog(serviceName);

// 📘 Ajout de Swagger pour générer la documentation OpenAPI
builder.Services.AddEndpointsApiExplorer(); // Permet d’explorer automatiquement les endpoints exposés par l’API
builder.Services.AddControllers();          // Active les contrôleurs MVC (API REST)

builder.Services.AddResponseCaching();      // 🗂️ Active le caching des réponses HTTP (cache côté client ou proxy)

// ⚙️ Configuration des services applicatifs
builder.Services
    .AddSwaggerAuthorizeSecurity(builder.Configuration)   // 🔐 Sécurise Swagger (authentification/autorisation sur la doc API)
    .AddDatabaseDbContext(builder.Configuration)          // 🗄️ Configure le DbContext EF Core (accès base de données)
    .AddAspNetCoreIdentity(builder.Configuration)         // 👥 Active ASP.NET Core Identity (utilisateurs, rôles, claims)
    .AddAuthentification(builder.Configuration)           // 🔑 Configure l’authentification (JWT, cookies, etc.)
    .AddCustomAuthorization(builder.Configuration)        // ✅ Configure l’autorisation (policies, rôles, claims)
    .AddServices(builder.Configuration)                   // 🛠️ Injection des services métiers (services applicatifs personnalisés)
    .AddMediatr(builder.Configuration)                    // 📬 Enregistre MediatR et scanne les handlers (CQRS : requêtes/commandes)
    .AddYarpResiliencePipeline()                          // 🔀 Ajoute un pipeline de résilience YARP (retry, circuit breaker, timeouts)
    .AddCustomOpenTelemetry(serviceName)                  // 📊 Configure OpenTelemetry (traces, métriques, logs pour observabilité)
    .AddSharedServices(builder.Configuration)             // ♻️ Ajoute les services partagés (middlewares, helpers communs)
    .AddReverseProxy()                                    // 🌐 Active le reverse proxy YARP (gestion des routes et clusters)
    .LoadFromMemory(                                      // 📋 Charge les routes et clusters YARP depuis une configuration en mémoire
        YarpGatewayConfig.GetRoutes(),                    // Routes définies dans YarpGatewayConfig (mapping des endpoints exposés)
        YarpGatewayConfig.GetClusters()                   // Clusters définis dans YarpGatewayConfig (backends vers lesquels router)
    )
    .AddTransforms<UserContextTransformProvider>();       // 🔧 Ajoute un TransformProvider personnalisé pour injecter le contexte utilisateur dans les requêtes proxifiées pour YARP


// 🚀 Construction de l’application
var app = builder.Build();

// 📝 Active Serilog pour capturer les logs applicatifs
app.UseCustomSerilogLogging(serviceName);

// 🛡️ Middleware global pour gérer les erreurs uniformément
app.UseGlobalErrorMiddleware();

// 📊 Endpoint Prometheus pour exporter les métriques OpenTelemetry
app.UseOpenTelemetryPrometheusScrapingEndpoint();

// 🗄️ Migration automatique de la base de données au démarrage
// Si la BD n’existe pas, elle sera créée et toutes les tables d’ASP.NET Identity seront générées
await app.AddDatabaseMigrationAsync<AppDbContext>(builder.Configuration);

// 🌍 Configuration du pipeline HTTP
if (app.Environment.IsDevelopment())
{
    // Swagger activé uniquement en développement
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mon Gateway");
        c.RoutePrefix = string.Empty; // Swagger accessible directement à la racine "/"
    });
}

// 🔑 Authentification → définit HttpContext.User
app.UseAuthentication();

// 👤 Middleware custom → enrichit le contexte utilisateur AVANT l’autorisation
app.UseMiddleware<UserContextMiddleware>();

// 🛡️ Autorisation → vérifie les droits via le contexte enrichi
app.UseAuthorization();

// 📍 Mapping des routes locales → endpoints REST prioritaires
app.MapControllers();

// 🗂️ Middleware de caching des réponses HTTP (cache côté client ou proxy)
app.UseResponseCaching();

// 🔀 Reverse Proxy YARP → dernier recours, protégé par autorisation
app.MapReverseProxy().RequireAuthorization();

// 🚀 Démarrage de l’application
app.Run();
```
- **Validation à 2 niveaux** :  
  - Request Validation : Forme et syntaxe (FluentValidation)  
  - Business Validation : Sémantique et état du système (IBusinessValidation)  
- **Global Error Handling** : Middleware interceptant toutes les exceptions pour un format de réponse `ApiResponse<T>` unique  
```csharp
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
```


---

## 📊 8. Observabilité (Stack LGT+S)
Le monitoring est au cœur de l'infrastructure
- 🔦 **Tempo & OpenTelemetry** : Tracing distribué de bout en bout *(Gateway → Kafka → DB)*  
- 📈 **Prometheus & Grafana** : Visualisation des métriques de santé et de performance  
- 🪵 **Loki & Seq** : Centralisation des logs structurés  
- 🩺 **Healthchecks** : Sondes de démarrage et de disponibilité pour chaque service et base de données
