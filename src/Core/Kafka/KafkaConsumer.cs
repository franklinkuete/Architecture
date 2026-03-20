using Confluent.Kafka;
using Core.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Core.Kafka;

/// <summary>
/// Service générique de consommation Kafka.
/// Gère la boucle d'écoute, la désérialisation JSON et l'exécution asynchrone des traitements.
/// </summary>
public class KafkaConsumer<T> : IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private bool _isDisposed;
    private readonly ILogger<KafkaConsumer<T>> _logger;

    public KafkaConsumer(KafkaSettings kafkaSettings, string overrideGroupId, ILogger<KafkaConsumer<T>> logger)
    {
        // Configuration du client : serveurs, groupe de consommation et comportement de lecture
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            GroupId = overrideGroupId,
            // Lit depuis le début si aucun offset n'est enregistré pour ce groupe
            AutoOffsetReset = AutoOffsetReset.Earliest,
            // Valide automatiquement la lecture des messages (Auto-Ack)
            EnableAutoCommit = true,
            // Réduit le temps d'attente lors de l'arrêt du service
            CancellationDelayMaxMs = 100
        };

        _logger = logger;
        // Initialisation du client natif
        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    /// <summary>
    /// Démarre la boucle de consommation sur un thread séparé pour ne pas bloquer l'application.
    /// </summary>
    /// <param name="topic">Le nom du topic à écouter</param>
    /// <param name="handleMessage">Le délégué (action) à exécuter pour chaque message reçu</param>
    /// <param name="ct">Token d'annulation lié au cycle de vie de l'application (BackgroundService)</param>
    public async Task ConsumeAsync(string topic, Func<T, Task> handleMessage, CancellationToken ct)
    {
        // S'abonne au topic spécifié
        _consumer.Subscribe(topic);

        var expectedTypeName = typeof(T).Name; // Ex: "CommandeCreatedEvent"

        // Task.Run est crucial ici pour libérer le thread principal au démarrage
        await Task.Run(async () =>
        {
            try
            {
                // Boucle infinie tant que l'application n'est pas arrêtée (via CancellationToken)
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        // Méthode bloquante qui attend un nouveau message ou le timeout du token
                        var cr = _consumer.Consume(ct);

                        if (cr?.Message?.Headers == null) continue;

                        // 1. Extraire le header "message-type"
                        var headerBytes = cr.Message.Headers.GetLastBytes("message-type");
                        if (headerBytes == null) continue;

                        var receivedTypeName = Encoding.UTF8.GetString(headerBytes);

                        // 2. FILTRAGE : Si ce n'est pas le bon type, on passe au suivant SANS erreur
                        if (receivedTypeName != expectedTypeName)
                        {
                            // Optionnel : Loguer que ce message est ignoré par ce groupe
                            continue;
                        }


                        if (cr?.Message?.Value != null)
                        {
                            T? message = default;
                            try
                            {
                                // Conversion du JSON brut en objet typé T
                                message = JsonSerializer.Deserialize<T>(cr.Message.Value, _jsonOptions);
                            }
                            catch (JsonException ex)
                            {
                                // Si le format est mauvais, on logue et on passe au suivant sans crasher la boucle
                                _logger.LogInformation($"Kafka : Échec de désérialisation du message de type {expectedTypeName} : {ex.Message}");
                                continue;
                            }

                            if (message != null)
                            {
                                _logger.LogInformation("Kafka : Message de type {MessageType} reçu et désérialisé avec succès.", expectedTypeName);
                                // Exécution du traitement métier (souvent un mediator.Send)
                                await handleMessage(message);
                            }
                        }
                    }
                    // Capture de l'arrêt normal via CancellationToken
                    catch (OperationCanceledException) { break; }
                    // Erreurs liées au protocole Kafka (réseau, authentification, etc.)
                    catch (ConsumeException e) { Console.WriteLine($"Erreur Kafka : {e.Error.Reason}"); }
                    // Erreurs logiques dans le traitement métier
                    catch (Exception ex) { _logger.LogError($"Kafka : Erreur logique lors du traitement : {ex.Message}");  }
                }
            }
            finally
            {
                // Libération propre : signale au cluster Kafka que ce membre quitte le groupe
                // Cela déclenche une redistribution (rebalance) immédiate des partitions
                this.Dispose();
            }
        }, ct);
    }

    /// <summary>
    /// Nettoie les ressources natives utilisées par le client Confluent.Kafka.
    /// </summary>
    public void Dispose()
    {

        if (_isDisposed) return;

        try
        {
            // On tente une fermeture propre seulement si ce n'est pas déjà fait
            _consumer.Close();
            _consumer.Dispose();
        }
        catch (ObjectDisposedException) { /* Ignorer si déjà détruit */ }
        finally
        {
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}




