using ClientApi.Infrastructure;
using Core.Extensions;
using Core.Middlewares;
using Core.Services;
using Mapster;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 📘 Ajout de Swagger pour générer la documentation OpenAPI
builder.Services.AddEndpointsApiExplorer(); // Permet d'explorer automatiquement les endpoints exposés par l'API
builder.Services.AddSwaggerGen(c =>
{
    // Déclare une version de la documentation Swagger (OpenAPI)
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API", // Titre affiché dans l'interface Swagger
        Version = "v1"    // Numéro de version de l'API
    });
});

// 🔖 Récupère le nom du service (assemblage) pour l'utiliser dans la configuration
var serviceName = typeof(Program).Assembly.GetName().Name!;

// 📝 Configure Serilog pour la journalisation centralisée et structurée
builder.ConfigureSerilog(serviceName);

// ⚙️ Configuration des services applicatifs
builder.Services
    .AddMediatr()                          // 📬 Enregistre MediatR et scanne les handlers (CQRS : requêtes/commandes)
    .AddRequestValidator()                 // ✅ Ajoute les validateurs de requêtes (FluentValidation)
    .AddBusinessValidator()                // 🧾 Ajoute les validateurs métier (Business rules)
    .AddMapster(builder.Configuration)     // 🔄 Configure Mapster pour le mapping DTO ↔ Domain
    .AddDatabaseDbContext(builder.Configuration) // 🗄️ Configure le DbContext EF Core pour la base de données
    .AddServices()                         // 📂 Injection des services métiers (repositories, services applicatifs)
    .AddCustomOpenTelemetry(serviceName)   // 📊 Configure OpenTelemetry pour la traçabilité et la supervision
    .AddSharedServices(builder.Configuration); // ♻️ Ajoute les services partagés (middlewares, helpers communs)

// 🎯 Ajout des contrôleurs MVC (API REST)
builder.Services.AddControllers();

// 📘 Ajout d’OpenAPI (nouvelle API Microsoft pour générer la doc)
builder.Services.AddOpenApi();

// 🚀 Construction de l’application
var app = builder.Build();

// 📝 Active Serilog pour capturer les logs applicatifs
app.UseCustomSerilogLogging(serviceName);

// 🛡️ Active ton middleware global partagé pour gérer les erreurs uniformément
app.UseGlobalErrorMiddleware();

// 🌍 Configuration du pipeline HTTP en mode développement
if (app.Environment.IsDevelopment())
{
    // Swagger activé uniquement en développement
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Endpoint de la doc Swagger
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Client API V1");
        c.RoutePrefix = ""; // Swagger accessible directement à la racine "/"
    });
}

// 🔑 Middleware d’autorisation (contrôle des accès)
app.UseAuthorization();

// 🎯 Mapping des contrôleurs MVC (routes REST)
app.MapControllers();

// 🚀 Démarrage de l’application
app.Run();
