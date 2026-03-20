using Core.Extensions;
using Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Serilog;
using Yarp.ReverseProxy.Forwarder;
using AppDbContext = Infrastructure.Data.AppDbContext;

namespace Infrastructure;

public static class InjectionDepencyInfrastructure
{
    // Enregistre mon DbContext dans le container d’injection de dépendances (DI) d’ASP.NET Core.
    public static IServiceCollection AddDatabaseDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")!;

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<DbContext, AppDbContext>();

        return services;
    }


    // Oblige Swagger à appliquer ce schéma de sécurité sur les endpoints qui nécessitent une authentification
    // Ajoute le bouton [Authorize] visible sur swagger et une fois authentifier toutes les requets vers les routes sécurisé (balise Authorize du controller) incluront le token
    // en realité l'attribut Authorization sera ajouté à toute les requetes avec le token (Authorization : bearer sjdh5JDQTDLLdhJ04"FD56DGSSF)

    public static IServiceCollection AddSwaggerAuthorizeSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            // Définition du schéma de sécurité Bearer
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            // Obligation d'utiliser ce schéma sur les routes protégées
            // TODO : A verifier et amélirer. Pas sûr que ca marche ce code
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearer", document)] = []
            });
        });

        return services;
    }

    // Configuration de ASP.NET Core Identity
    public static IServiceCollection AddAspNetCoreIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        // Active le système d’authentification/gestion des utilisateurs et rôles fourni par ASP.NET Core Identity
        // .AddEntityFrameworkStores<DbContext>() Indique à Identity d’utiliser Entity Framework Core pour stocker les utilisateurs et rôles dans ta base SQL
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        }).AddEntityFrameworkStores<AppDbContext>()
          .AddDefaultTokenProviders();

        return services;
    }

    
    // configure l’authentification JWT (JSON Web Token) 
    public static IServiceCollection AddAuthentification(this IServiceCollection services, IConfiguration configuration)
    {
        var audience = configuration["JwtSettings:Audience"];
        var issuer = configuration["JwtSettings:Issuer"];
        var secretKey = configuration["JwtSettings:PrivateKey"]!;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
           .AddJwtBearer(options =>
           {
               options.SaveToken = true;
               options.RequireHttpsMetadata = false;
               options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
               {
                   ValidateIssuer = true,
                   ValidateAudience = false,
                   ValidateLifetime = true,
                   ValidateIssuerSigningKey = true,
                   ValidAudience = audience,
                   ValidIssuer = issuer,
                   IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey))
               };
           });
        return services;
    }

    // Ajout des services
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IForwarderHttpClientFactory, PollyForwarderHttpClientFactory>();
        services.AddScoped<IUserContext, UserContext>();

        return services;
    }

    // Ajouter Mediatr (Assemby Application et tout les handler CQRS)
    public static IServiceCollection AddMediatr(this IServiceCollection services, IConfiguration configuration)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName != null && a.FullName.StartsWith("Application"))
            .ToArray();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));

        return services;
    }

    // configure l'authorization en définissant les politiques pour les roles
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("SuperAdministratorPolicy", policy => policy.RequireRole(Constante.Role.SUPERADMIN));
            options.AddPolicy("AdministratorPolicy", policy => policy.RequireRole(Constante.Role.ADMINISTRATOR));
        });
        return services;
    }

    // effectue la migration de la base de données (création de la bd et des tables de sécurité Asp.NET Identity)
    // création du premier utilisateur admin
    public static async Task<IHost> AddDatabaseMigrationAsync<T>(this IHost host, IConfiguration configuration) where T : DbContext
    {
        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            var db = services.GetRequiredService<T>();

            //si toute les table de asp.net identity sont déjà crée et qu'on peut se connecter à la bd, pas besoin de faire la migration à chaque fois
            if (!db.Database.CanConnect())
            {
                // infos de pour créer l'administrateur
                // dans un environnement conteneurisé, ces infos seront remplacé par ceux du fichier .env
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                var adminName = configuration["SuperAdminSettings:SuperAdminUserName"]!;
                var adminEmail = configuration["SuperAdminSettings:SuperAdminEmail"]!;
                var adminPassword = configuration["SuperAdminSettings:SuperAdminPassword"]!;
                var roleName = Constante.Role.SUPERADMIN;

                Log.Information("{@prefix}🚀Migration de la base de données", Constante.Prefix.DBPrefix);
                try
                {
                    // This will apply any pending migrations and create the database if it does not already exist
                    db.Database.Migrate();
                    Log.Information("{@prefix}📦La base de données {@db} a été crée", 
                        Constante.Prefix.DBPrefix,
                        db.Database.GetDbConnection().Database);
                    Log.Information("{@prefix}📦Informations de la base de données : {@database}", Constante.Prefix.DBPrefix, db.Database);

                    // creation du 1er utilisateur admin
                    // Création du premier utilisateur admin
                    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();


                    // on vérifie si le rôle administrateur existe déjà
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        // s'il n'éxiste pas, on le crée
                        var role = new IdentityRole
                        {
                            Name = roleName,
                            NormalizedName = roleName.ToUpperInvariant()
                        };

                        // création du role
                        var resultRole = await roleManager.CreateAsync(role);

                        // si le role est créer on crée l'utilisateur admin et on l'affecte au role
                        if (resultRole.Succeeded)
                        {
                            var existingAdmin = await userManager.FindByNameAsync(adminName);

                            // si l'administrateur n'existe pas on le creer
                            if (existingAdmin == null)
                            {
                                var adminUser = new IdentityUser
                                {
                                    UserName = adminName,
                                    Email = adminEmail,
                                    EmailConfirmed = true,
                                };

                                Log.Information("{@prefix}📦Création du premier utilisateur/administrateur {@admin} démarré",
                                    Constante.Prefix.DBPrefix,
                                    adminName);

                                var result = await userManager.CreateAsync(adminUser, adminPassword);

                                if (result.Succeeded)
                                {
                                    Log.Information("{@prefix}📦Super Administrateur {@adminName} a été crée avec succès : {@user}",
                                        Constante.Prefix.DBPrefix,
                                        adminName,
                                        adminUser);
                                    await userManager.AddToRoleAsync(adminUser, roleName);
                                    Log.Information("{@prefix}📦Super Administrateur {@adminName} a été affecté au rôle {@roleName}",
                                         Constante.Prefix.DBPrefix,
                                        adminName,
                                        roleName);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Fatal("{@prefix}❌Une erreur est survenue lors de l'initialisation de la base de données : {@ex}",
                        Constante.Prefix.DBPrefix,
                        ex);
                }
            }
            else
            {
                Log.Information("{@prefix}✅La Connexion à la base de données de Asp.NET Identity a été établie : {@database}",
                      Constante.Prefix.DBPrefix,
                    db.Database.GetDbConnection().Database);
            }

        }
        return host;
    }

}

