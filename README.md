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

## 📊 9. Docker-compose.yml
Ci-joint mes conteneurs docker : 
```yaml
services:

  # =========================
  # SEQ : serveur de logs centralisé
  # =========================
  seq:
    image: datalust/seq:latest
    container_name: seq
    restart: unless-stopped
    ports:
      - "5341:80"   # Port externe 5341 mappé sur le port interne 80 du conteneur.
                    # => Accessible via http://localhost:5341
    environment:
      ACCEPT_EULA: Y   # Obligatoire pour accepter la licence SEQ.
      SEQ_FIRSTRUN_ADMINPASSWORD: "admin123"  # Mot de passe admin initial (à changer en production).
    networks:
        - docker-container-network

  # =========================
  # API Gateway : point d’entrée unique
  # =========================
  apigateway:
    image: ${DOCKER_REGISTRY-}apigateway
    build:
      context: .
      dockerfile: src/apigateway-microservice/ApiGateway/Dockerfile
    ports:
      - "5000:80"   # Port externe 5000 redirigé vers le port interne 80 (HTTP).
      - "5001:443"  # Port externe 5001 redirigé vers le port interne 443 (HTTPS).
    depends_on:
      - loki        # Nécessite Loki pour les logs.
      - tempo       # Nécessite Tempo pour le tracing distribué.
      - prometheus  # Nécessite Prometheus pour les métriques.
      - db          # Dépend de SQL Server.
      - seq         # Dépend de SEQ pour les logs.
    environment:
      # Chaîne de connexion SQL Server (utilise les variables du .env)
      - ConnectionStrings__DefaultConnection=Server=db,1433;Database=${DATABASE};User=sa;Password=${SA_PASSWORD};Encrypt=True;TrustServerCertificate=True;
      # Paramètres JWT pour l’authentification
      - JwtSettings__PrivateKey=${PrivateKey}   # Clé privée utilisée pour signer les tokens.
      - JwtSettings__Issuer=${Issuer}           # Émetteur du token.
      - JwtSettings__Audience=${Audience}       # Audience cible du token.
      - JwtSettings__ExpiryMinutes=${ExpiryMinutes} # Durée de vie du token en minutes.
      # Paramètres du super administrateur
      - SuperAdminSettings__SuperAdminUserName=${SuperAdminUserName}
      - SuperAdminSettings__SuperAdminEmail=${SuperAdminEmail}
      - SuperAdminSettings__SuperAdminPassword=${SuperAdminPassword}
      # Configuration ASP.NET Core
      - ASPNETCORE_URLS=http://+:80             # Le service écoute sur le port interne 80.
      - ASPNETCORE_ENVIRONMENT=Development      # Mode développement (swagger activé).
      # Configuration Serilog pour Loki
      - Serilog__WriteTo__1__Name=GrafanaLoki   # Active l’écriture des logs vers Loki.
      - Serilog__WriteTo__1__Args__uri=http://loki:3100 # URL de Loki pour recevoir les logs.
      # Export des traces vers Tempo (OpenTelemetry)
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://tempo:4317
    networks:
        - docker-container-network

  # =========================
  # Client API : microservice lié aux clients
  # =========================
  clientapi:
    image: ${DOCKER_REGISTRY-}clientapi
    build:
      context: .
      dockerfile: src/client-microservice/ClientApi/Dockerfile
    ports:
      - "4080:80"   # Port externe 4080 redirigé vers le port interne 80.
                    # => Accessible via http://localhost:4080
    environment:
      # Chaîne de connexion Postgres (variables du .env injectées)
      - ConnectionStrings__DefaultConnection=Server=postgres,5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};
      # Configuration ASP.NET Core
      - ASPNETCORE_URLS=http://+:80             # Le service écoute sur le port interne 80.
      - ASPNETCORE_ENVIRONMENT=Development      # Mode développement (swagger activé).
    depends_on:
      - loki
      - tempo
      - prometheus
      - db
      - seq
    networks:
        - docker-container-network

  # =========================
  # Product API : microservice lié aux produits
  # =========================
  productapi:
    image: ${DOCKER_REGISTRY-}productapi
    build:
      context: .
      dockerfile: src/product-microservice/ProductApi/Dockerfile
    ports:
      - "4081:8080"   # Port externe 4081 redirigé vers le port interne 8080.
                      # => Accessible via http://localhost:4081
    environment:
      # Chaîne de connexion MySQL
      - ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=${MYSQL_DATABASE};User=root;Password=${MYSQL_ROOT_PASSWORD};
      # Configuration ASP.NET Core
      - ASPNETCORE_URLS=http://+:8080           # Le service écoute sur le port interne 8080.
      - ASPNETCORE_ENVIRONMENT=Development      # Mode développement (swagger activé).
      # Kafka pour la communication asynchrone
      - KafkaSettings__BootstrapServers=kafka:29092 # Adresse du broker Kafka interne.
    depends_on:
      - loki
      - tempo
      - prometheus
      - db
      - seq
      - kafka
    networks:
        - docker-container-network

  # =========================
  # Commande API : microservice lié aux commandes
  # =========================
  commandeapi:
    image: ${DOCKER_REGISTRY-}commandeapi
    build:
      context: .
      dockerfile: src/commande-microservice/CommandeApi/Dockerfile
    ports:
      - "4082:8080"   # Port externe 4082 redirigé vers le port interne 8080.
                      # => Accessible via http://localhost:4082
    environment:
      # Chaîne de connexion MariaDB
      - MariaDbSetting__ConnectionString=Server=mariadb;Port=3306;Database=${MARIADB_DATABASE};Uid=${MARIADB_USER};Pwd=${MARIADB_PASSWORD}
      - MariaDbSetting__DatabaseName=CommandeDb # Nom de la base utilisée par ce microservice.
      # Configuration ASP.NET Core
      - ASPNETCORE_URLS=http://+:8080           # Le service écoute sur le port interne 8080.
      - ASPNETCORE_ENVIRONMENT=Development      # Mode développement (swagger activé).
      # Kafka pour la communication asynchrone
      - KafkaSettings__BootstrapServers=kafka:29092
    depends_on:
      - mariadb  # Dépendance directe à MariaDB (doit être démarré avant).
      - loki
      - tempo
      - prometheus
      - db
      - kafka
    networks:
        - docker-container-network

  # =========================
  # Grafana : visualisation des métriques
  # =========================
  grafana:
    image: grafana/grafana:10.4.0
    container_name: grafana
    ports:
      - "3000:3000"   # Port externe 3000 mappé sur le port interne 3000.
                      # => Accessible via http://localhost:3000
    volumes:
      - ./grafana/provisioning/datasources:/etc/grafana/provisioning/datasources
                      # Montage des datasources pour provisionner automatiquement les connexions (Prometheus, Loki, Tempo).
    environment:
      - GF_SECURITY_ADMIN_USER=admin     # Identifiant admin par défaut (à changer en production).
      - GF_SECURITY_ADMIN_PASSWORD=admin # Mot de passe admin par défaut (à changer en production).
    depends_on:
      - loki
      - tempo
      - prometheus
    networks:
        - docker-container-network

  # =========================
  # Prometheus : collecte des métriques
  # =========================
  prometheus:
    image: prom/prometheus:v2.52.0
    container_name: prometheus
    command:
      - "--config.file=/etc/prometheus/prometheus.yml" # Fichier de configuration Prometheus.
      - "--storage.tsdb.retention.time=15d"            # Rétention des métriques pendant 15 jours.
    ports:
      - "9090:9090"   # Port externe 9090 mappé sur le port interne 9090.
                      # => Accessible via http://localhost:9090
    volumes:
      - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml # Montage du fichier de configuration.
    networks:
        - docker-container-network

  # =========================
  # Tempo : tracing distribué
  # =========================
  tempo:
    image: grafana/tempo:2.4.1
    container_name: tempo
    command: [ "-config.file=/etc/tempo/tempo.yml" ] # Fichier de configuration Tempo.
    ports:
      - "3200:3200"   # Port externe 3200 mappé sur le port interne 3200 (API Tempo).
      - "4316:4316"   # Port externe 4316 mappé sur le port interne 4316 (OTLP gRPC).
    volumes:
      - ./tempo/tempo.yml:/etc/tempo/tempo.yml # Montage du fichier de configuration.
    networks:
        - docker-container-network

  # =========================
  # Loki : gestion des logs
  # =========================
  loki:
    image: grafana/loki:2.9.4
    container_name: loki
    command: [ "-config.file=/etc/loki/loki-config.yml", "-config.expand-env=true" ]
                      # Ajout du flag -config.expand-env=true pour permettre l’utilisation des variables d’environnement dans le fichier YAML.
    ports:
      - "3100:3100"   # Port externe 3100 mappé sur le port interne 3100.
                      # => Accessible via http://localhost:3100
    volumes:
      - ./loki/loki-config.yml:/etc/loki/loki-config.yml # Montage du fichier de configuration.
    networks:
        - docker-container-network

  # =========================
  # SQL Server : base relationnelle
  # =========================
  db:
    image: mcr.microsoft.com/mssql/server:2025-latest
    container_name: sqlserver2025
    ports:
      - "1433:1433"   # Port externe 1433 mappé sur le port interne 1433 (SQL Server).
    environment:
      - ACCEPT_EULA=Y                   # Acceptation de la licence SQL Server.
      - MSSQL_SA_PASSWORD=${SA_PASSWORD} # Mot de passe de l’utilisateur SA (défini dans .env).
    volumes:
      - sql_data:/var/opt/mssql          # Volume persistant pour stocker les données SQL Server.
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P ${SA_PASSWORD} -Q 'SELECT 1'"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped
    networks:
        - docker-container-network

  # =========================
  # Postgres : base relationnelle
  # =========================
  postgres:
    image: postgres:16
    container_name: postgres-db
    restart: unless-stopped
    ports:
      - "5432:5432"   # Port externe 5432 mappé sur le port interne 5432 (Postgres).
    environment:
      POSTGRES_DB: ${POSTGRES_DB}       # Nom de la base Postgres.
      POSTGRES_USER: ${POSTGRES_USER}   # Utilisateur Postgres.
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD} # Mot de passe Postgres.
    volumes:
      - pg_data:/var/lib/postgresql/data # Volume persistant pour Postgres.
      - ./sql-postgres-clientdb:/docker-entrypoint-initdb.d/:ro # Script d’initialisation.
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
        - docker-container-network

  # =========================
  # MySQL : base relationnelle
  # =========================
  mysql:
    image: mysql:8.3
    container_name: mysql-db
    restart: unless-stopped
    ports:
      - "3306:3306"   # Port externe 3306 mappé sur le port interne 3306 (MySQL).
    environment:
      MYSQL_DATABASE: ${MYSQL_DATABASE}       # Nom de la base MySQL.
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD} # Mot de passe root MySQL.
    volumes:
      - mysql_data:/var/lib/mysql             # Volume persistant pour MySQL.
      - ./sql-mysql-productdb:/docker-entrypoint-initdb.d/:ro # Script d’initialisation.
    healthcheck:
      test: ["CMD-SHELL", "mysqladmin ping -h localhost -u root -p${MYSQL_ROOT_PASSWORD}"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
        - docker-container-network


  # =========================
  # MariaDB : base relationnelle
  # =========================
  mariadb:
    image: mariadb:10.11
    container_name: mariadb
    restart: always
    ports:
      - "3307:3306"   # Port externe 3307 mappé sur le port interne 3306 (MariaDB).
                      # => Accessible via localhost:3307
    environment:
      MARIADB_ROOT_PASSWORD: ${MARIADB_ROOT_PASSWORD} # Mot de passe root MariaDB (variable .env).
      MARIADB_DATABASE: ${MARIADB_DATABASE}           # Nom de la base MariaDB (variable .env).
      MARIADB_USER: ${MARIADB_USER}                   # Utilisateur MariaDB (variable .env).
      MARIADB_PASSWORD: ${MARIADB_PASSWORD}           # Mot de passe utilisateur MariaDB (variable .env).
    volumes:
      - mariadb_storage:/var/lib/mysql                # Volume persistant pour MariaDB.
      - ./sql-mariadb-commandedb:/docker-entrypoint-initdb.d/:ro # Script d’initialisation.
    healthcheck:
      test: ["CMD-SHELL", "mariadb -u root -p$MARIADB_ROOT_PASSWORD -e 'SELECT 1;'"]
      interval: 5s
      timeout: 5s
      retries: 10
    networks:
        - docker-container-network

  # =========================
  # Redis : cache distribué
  # =========================
  redis:
    image: redis:7.2
    container_name: redis_cache
    ports:
      - "6379:6379"   # Port externe 6379 mappé sur le port interne 6379 (Redis).
                      # => Accessible via localhost:6379
    networks:
        - docker-container-network

  # =========================
  # Redis Commander : interface web pour Redis
  # =========================
  redis-commander:
    image: rediscommander/redis-commander:latest
    container_name: redis_commander
    ports:
      - "8078:8081"   # Port externe 8078 mappé sur le port interne 8081.
                      # => Accessible via http://localhost:8078
    environment:
      - REDIS_HOSTS=local:redis_cache:6379 # Connexion au service Redis défini ci-dessus.
    depends_on:
      - redis
    networks:
        - docker-container-network

  # =========================
  # Kafka : bus de messages
  # =========================
  kafka:
    image: confluentinc/cp-kafka:latest
    container_name: kafka
    ports:
      - "29092:29092" # Port externe 29092 mappé sur le port interne 29092 (Kafka externe).
                      # => Accessible via localhost:29092
    environment:
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: broker,controller
      KAFKA_CONTROLLER_QUORUM_VOTERS: 1@localhost:9093
      KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER

      # Listeners pour Kafka (interne/externe/controller)
      KAFKA_LISTENERS: INTERNAL://0.0.0.0:9092,EXTERNAL://0.0.0.0:29092,CONTROLLER://0.0.0.0:9093

      # Publicité des listeners (interne/externe)
      KAFKA_ADVERTISED_LISTENERS: INTERNAL://kafka:9092,EXTERNAL://kafka:29092

      # Mapping des protocoles de sécurité
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,INTERNAL:PLAINTEXT,EXTERNAL:PLAINTEXT

      # Utilisation du réseau interne Docker pour l’inter-broker
      KAFKA_INTER_BROKER_LISTENER_NAME: INTERNAL

      # Paramètres de réplication et de tolérance
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1

      # Création automatique des topics activée
      KAFKA_AUTO_CREATE_TOPICS_ENABLE: "true"

      # Identifiant du cluster Kafka
      CLUSTER_ID: "MkU3OEVBNTcwNTJENDM2Qk"

    networks:
        - docker-container-network


volumes:
  sql_data:          # Volume persistant pour SQL Server (stockage des données de la base).
  pg_data:           # Volume persistant pour PostgreSQL (stockage des données de la base).
  mysql_data:        # Volume persistant pour MySQL (stockage des données de la base).
  kafka_data:        # Volume persistant pour Kafka (stockage des logs et métadonnées du broker).
  mariadb_storage:   # Volume persistant pour MariaDB (stockage des données de la base).

networks:
  docker-container-network:   # Déclaration d’un réseau Docker personnalisé
    driver: bridge            # Type de réseau : "bridge"
                              # => C’est le mode par défaut pour les conteneurs,
                              #    mais ici on crée un réseau nommé pour mieux contrôler.
                              # 
                              # Avantages :
                              # - Tous les services attachés à ce réseau peuvent communiquer
                              #   directement entre eux par leur nom de service (DNS interne Docker).
                              # - Pas besoin d’IP fixes : Docker résout automatiquement "redis", "seq", "loki", etc.
                              # - Isolation : les conteneurs ne sont pas exposés sur le réseau par défaut,
                              #   uniquement sur ce réseau dédié.
                              # - Performances : réduit la latence inter‑conteneurs en évitant des passerelles NAT multiples.
                              #
                              # Bonnes pratiques :
                              # - Attacher tous tes microservices (API Gateway, Redis, Kafka, Loki, Seq, etc.)
                              #   à ce réseau pour une communication interne rapide et sécurisée.
                              # - N’exposer vers l’hôte que les ports nécessaires (ex. YARP 5000/5001, Grafana 3000).
```
