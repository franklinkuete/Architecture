using Core.Extensions;
using Core.Middlewares;
using Core.Services;
using Microsoft.OpenApi;
using ProductApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 📘 Ajout de Swagger pour générer la documentation OpenAPI
builder.Services.AddEndpointsApiExplorer(); // Permet d’explorer automatiquement les endpoints exposés par l’API
builder.Services.AddSwaggerGen(c =>
{
    // Déclare une version de la documentation Swagger (OpenAPI)
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My Product API", // Titre affiché dans l’interface Swagger
        Version = "v1"            // Numéro de version de l’API
    });
});

// 🔖 Récupère le nom du service (assemblage) pour l’utiliser dans la configuration
var serviceName = typeof(Program).Assembly.GetName().Name!;

// 🎯 Ajout des contrôleurs MVC (API REST)
builder.Services.AddControllers();

// 📘 Ajout d’OpenAPI (nouvelle API Microsoft pour générer la doc)
builder.Services.AddOpenApi();

// 📝 Configure Serilog pour la journalisation centralisée et structurée
builder.ConfigureSerilog(serviceName);

// ⚙️ Ajout des services applicatifs via la couche Infrastructure
builder.Services
   .AddDatabaseDbContext(builder.Configuration)   // 🗄️ Configure le DbContext EF Core (base de données relationnelle)
   .AddServices()                                // 📂 Injection des services métiers et repositories
   .AddCustomOpenTelemetry(serviceName)          // 📊 Configure OpenTelemetry pour la traçabilité et la supervision
   .AddKafkaConsumerEvent(builder.Configuration) // 📡 Ajoute un consommateur Kafka pour traiter les événements asynchrones
   .AddBusinessValidator()                       // 🧾 Ajoute les validateurs métier (Business rules)
   .AddRequestValidator()                        // ✅ Ajoute les validateurs de requêtes (Data annotations, FluentValidation, etc.
   .AddMassTransit()                             // 🚍 Configure MassTransit pour la gestion des messages/queues
   .AddMapster()                                 // 🔄 Configure Mapster pour le mapping DTO ↔ Domain
   .AddMediatr()                                 // 📬 Enregistre MediatR et scanne les handlers (CQRS, requêtes/commandes)
   .AddSharedServices(builder.Configuration);    // ♻️ Ajoute les services partagés (middlewares, helpers communs)

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
    // app.MapOpenApi(); // option alternative pour exposer OpenAPI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Endpoint de la doc Swagger
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API V1");
        c.RoutePrefix = ""; // Swagger accessible directement à la racine "/"
    });
}

// 🔑 Middleware d’autorisation (contrôle des accès)
app.UseAuthorization();

// 🎯 Mapping des contrôleurs MVC (routes REST)
app.MapControllers();

// 🚀 Démarrage de l’application
app.Run();