using Confluent.Kafka;
using Core.Configuration;
using System.Text;
using System.Text.Json;


namespace Core.Kafka;

/// <summary>
/// Service responsable de l'envoi de messages vers Kafka.
/// Implémente IDisposable pour libérer les ressources natives de la bibliothèque Confluent.Kafka.
/// </summary>
public class KafkaProducer : IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaProducer(KafkaSettings kafkaSettings)
    {
        // Configuration du producteur : serveurs, logs de debug et timeout des messages
        var config = new ProducerConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            Debug = kafkaSettings.Debug,
            MessageTimeoutMs = kafkaSettings.MessageTimeoutMs
        };

        // Initialisation du producteur avec des clés et valeurs de type string
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    /// <summary>
    /// Envoie un message de manière asynchrone vers un topic spécifique.
    /// </summary>
    /// <typeparam name="T">Le type de l'objet à sérialiser en JSON</typeparam>
    /// <param name="topic">Nom du topic Kafka cible</param>
    /// <param name="key">Clé du message (utile pour garantir l'ordre dans les partitions)</param>
    /// <param name="message">L'objet à envoyer</param>
    public async Task ProduceAsync<T>(string topic, string key, T message)
    {
        try
        {
            // 1. CRÉATION DES MÉTADONNÉES (HEADERS)
            // On utilise les Headers pour "étiqueter"/"marquer" le message sans toucher au corps du JSON.
            var headers = new Headers();

            // On ajoute le nom de la classe technique (ex: "CommandeCreatedEvent") dans le header.
            // Cela permet au consommateur de filtrer le message SANS avoir à désérialiser le JSON,
            // ce qui est beaucoup plus performant et évite les erreurs de format inutiles.
            headers.Add("message-type", Encoding.UTF8.GetBytes(typeof(T).Name));

            // 2. SÉRIALISATION DU CORPS DU MESSAGE
            // Transformation de l'objet .NET en chaîne de caractères JSON.
            var value = JsonSerializer.Serialize(message);

            // 3. CONSTRUCTION DU PAQUET KAFKA
            var kafkaMessage = new Message<string, string>
            {
                Key = key,      // La clé permet de garantir l'ordre des messages (même clé = même partition)
                Value = value,  // Le contenu métier
                Headers = headers // Les métadonnées de routage/type
            };

            // 4. ENVOI ASYNCHRONE VERS LE BROKER
            // ProduceAsync attend que Kafka renvoie un accusé de réception (ACK).
            // Le comportement dépend de votre config 'Acks' (All, 1, ou 0).
            var report = await _producer.ProduceAsync(topic, kafkaMessage);

            // 5. VÉRIFICATION DE LA DISPONIBILITÉ RÉELLE (PERSISTENCE)
            // Contrairement à un simple "Success", on vérifie ici le statut de persistence.
            // 'Persisted' signifie que le message est écrit sur le disque du leader et/ou des réplicas.
            if (report.Status != PersistenceStatus.Persisted)
            {
                // Si le statut est 'PossiblyPersisted' ou 'NotPersisted', il y a un risque de perte de données.
                // C'est ici qu'on logue une alerte critique pour la surveillance (Monitoring).
                Console.WriteLine($"Attention : Message non persisté. Statut : {report.Status}");
            }

        }
        catch (ProduceException<string, string> e)
        {
            // Erreur spécifique à Kafka (ex: problème réseau, topic inexistant, timeout)
            Console.WriteLine($"Échec de livraison Kafka : {e.Error.Reason}");
            throw; // Propager l'exception pour gestion dans le Handler appelant
        }
    }

    /// <summary>
    /// Libère les ressources du producteur lors de la destruction de l'objet.
    /// </summary>
    public void Dispose()
    {
        // Force l'envoi des messages restés dans le buffer interne avant de couper
        _producer?.Flush(TimeSpan.FromSeconds(10));

        // Libère les handles natifs et ferme les connexions TCP
        _producer?.Dispose();
    }
}


