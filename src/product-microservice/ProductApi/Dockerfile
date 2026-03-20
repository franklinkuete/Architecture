# Consultez https://aka.ms/customizecontainer pour savoir comment personnaliser votre conteneur de débogage et comment Visual Studio utilise ce Dockerfile pour générer vos images afin d’accélérer le débogage.

# Cet index est utilisé lors de l’exécution à partir de VS en mode rapide (par défaut pour la configuration de débogage)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Cette phase est utilisée pour générer le projet de service
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/product-microservice/ProductApi/ProductApi.csproj", "src/product-microservice/ProductApi/"]
COPY ["src/apigateway-microservice/Infrastructure/Infrastructure.csproj", "src/apigateway-microservice/Infrastructure/"]
COPY ["src/Core/Core.csproj", "src/Core/"]
COPY ["src/product-microservice/ProductApi.Application/ProductApi.Application.csproj", "src/product-microservice/ProductApi.Application/"]
COPY ["src/product-microservice/ProductApi.Domain/ProductApi.Domain.csproj", "src/product-microservice/ProductApi.Domain/"]
COPY ["src/product-microservice/ProductApi.Infrastructure/ProductApi.Infrastructure.csproj", "src/product-microservice/ProductApi.Infrastructure/"]
RUN dotnet restore "./src/product-microservice/ProductApi/ProductApi.csproj"
COPY . .
WORKDIR "/src/src/product-microservice/ProductApi"
RUN dotnet build "./ProductApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Cette étape permet de publier le projet de service à copier dans la phase finale
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProductApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Cette phase est utilisée en production ou lors de l’exécution à partir de VS en mode normal (par défaut quand la configuration de débogage n’est pas utilisée)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductApi.dll"]