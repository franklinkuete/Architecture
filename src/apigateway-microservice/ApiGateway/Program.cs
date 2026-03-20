using ApiGateway;
using Core.Extensions;
using Core.Middlewares;
using Core.Services;
using Infrastructure;
using Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

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

