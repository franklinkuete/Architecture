using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;
using static System.Collections.Specialized.BitVector32;
using DbContext = Infrastructure.Data.DbContext;






namespace Infrastructure
{
    public static class InjectionDepencyInfrastructure
    {
        public static IServiceCollection InjectDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<DbContext>(options => options.UseSqlServer(connectionString));

            return services;
        }


        // Configuration de ASP.NET Core Identity
        public static IServiceCollection ConfigureIdentity(this IServiceCollection services, IConfiguration configuration)
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
            }).AddEntityFrameworkStores<DbContext>()
              .AddDefaultTokenProviders();


            return services;
        }
        public static IServiceCollection ConfigureAuthentification(this IServiceCollection services, IConfiguration configuration)
        {
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
                       ValidAudience = configuration["JwtSettings:Audience"],
                       ValidIssuer = configuration["JwtSettings:Issuer"],
                       IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!))
                   };
               });
            return services;
        }

        public static IServiceCollection ConfigureAuthorisation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdministratorPolicy", policy => policy.RequireRole("Administrator"));
                options.AddPolicy("UserPolicy", policy => policy.RequireRole("User"));
            });
            return services;
        }

    }
}
