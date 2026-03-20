# Consultez https://aka.ms/customizecontainer pour savoir comment personnaliser votre conteneur de débogage et comment Visual Studio utilise ce Dockerfile pour générer vos images afin d’accélérer le débogage.

# Cet index est utilisé lors de l’exécution à partir de VS en mode rapide (par défaut pour la configuration de débogage)
# LORSQUE VS STUDIO GENERE LES DOCKERFILE IL EXPOSE LES PORTS INTERNES DU CONTENEUR (http=8080,https=8081) (QUI PEUVENT ETRE REDEFINI DANS LE DOCKER-COMPOSE DANS LES VARIABLE D'ENVI DU CONTENEUR)
# TOUT COMME LES VARIABLES D'ENVIRONNEMENT INITIALEMENT DEFINIES DANS LAUNCHSETTINGS.JSON
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Cette phase est utilisée pour générer le projet de service
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/client-microservice/ClientApi/ClientApi.csproj", "src/client-microservice/ClientApi/"]
COPY ["src/apigateway-microservice/Infrastructure/Infrastructure.csproj", "src/apigateway-microservice/Infrastructure/"]
COPY ["src/client-microservice/Core/Core/Core.csproj", "src/client-microservice/Core/Core/"]
COPY ["src/client-microservice/ClientApi.Infrastructure/ClientApi.Infrastructure.csproj", "src/client-microservice/ClientApi.Infrastructure/"]
COPY ["src/apigateway-microservice/Application/Application.csproj", "src/apigateway-microservice/Application/"]
COPY ["src/apigateway-microservice/Domaine/Domaine.csproj", "src/apigateway-microservice/Domaine/"]
COPY ["src/client-microservice/ClientApi.Application/ClientApi.Application.csproj", "src/client-microservice/ClientApi.Application/"]
COPY ["src/client-microservice/ClientApi.Domain/ClientApi.Domain.csproj", "src/client-microservice/ClientApi.Domain/"]
RUN dotnet restore "./src/client-microservice/ClientApi/ClientApi.csproj"
COPY . .
WORKDIR "/src/src/client-microservice/ClientApi"
RUN dotnet build "./ClientApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Cette étape permet de publier le projet de service à copier dans la phase finale
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ClientApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Cette phase est utilisée en production ou lors de l’exécution à partir de VS en mode normal (par défaut quand la configuration de débogage n’est pas utilisée)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClientApi.dll"]