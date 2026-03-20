using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder; // Pour ForwarderRequestConfig
using System.Net;

namespace ApiGateway;

public static class YarpGatewayConfig
{
    public static IReadOnlyList<RouteConfig> GetRoutes() => new List<RouteConfig>
{


    new RouteConfig
    {
        RouteId = "ClientRoute",
        ClusterId = "ClientServiceCluster",
        Match = new RouteMatch { Path = "/api/Client/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "ProductRoute",
        ClusterId = "ProductServiceCluster",
        Match = new RouteMatch { Path = "/api/Product/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "CategorieRoute",
        ClusterId = "CategorieServiceCluster",
        Match = new RouteMatch { Path = "/api/Categorie/{**catch-all}" }
    }
,
    new RouteConfig
    {
        RouteId = "CommandeRoute",
        ClusterId = "CommandeServiceCluster",
        Match = new RouteMatch { Path = "/api/Commande/{**catch-all}" }
    }
};

    public static IReadOnlyList<ClusterConfig> GetClusters()
    {
        // On définit la configuration pour forcer le protocole HTTP/1.1
        // C'est le remède miracle pour supprimer la latence de négociation dans Docker
        var http11Config = new ForwarderRequestConfig
        {
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            // OPTIMISATION : Augmenter le timeout pour éviter les retries prématurés
            ActivityTimeout = TimeSpan.FromSeconds(60)
        };

        return new List<ClusterConfig>
    {
        new ClusterConfig
        {
            ClusterId = "CategorieServiceCluster",
            HttpRequest = http11Config, // 👈 Ajouté ici
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["CategorieService"] = new DestinationConfig { Address = "http://productapi:8080" }
            }
        },

        new ClusterConfig
        {
            ClusterId = "ClientServiceCluster",
            HttpRequest = http11Config, // 👈 Ajouté ici
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["ClientService"] = new DestinationConfig { Address = "http://clientapi:80" }
            }
        },
        new ClusterConfig
        {
            ClusterId = "ProductServiceCluster",
            HttpRequest = http11Config, // 👈 Ajouté ici
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["ProductService"] = new DestinationConfig { Address = "http://productapi:8080" }
            }
        },
        new ClusterConfig
        {
            ClusterId = "CommandeServiceCluster",
            HttpRequest = http11Config, // 👈 Ajouté ici
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["CommandeService"] = new DestinationConfig { Address = "http://commandeapi:8080" }
            }
        }
    };
    }

}
