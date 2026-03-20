using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

public class RedisConnectionService
{
    private readonly ILogger<RedisConnectionService> _logger;
    private readonly string _connectionString;

    public RedisConnectionService(ILogger<RedisConnectionService> logger, IConfiguration config)
    {
        _logger = logger;
        _connectionString = "redis:6379";
    }

    public IConnectionMultiplexer CreateConnection()
    {
        var connection = ConnectionMultiplexer.Connect(_connectionString);

        try
        {
            var db = connection.GetDatabase();
            var pong = db.Ping();

            // Log d’information avec la latence
            _logger.LogInformation("Redis connecté, latence = {Latency} ms", pong.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            // Log d’erreur si Redis ne répond pas
            _logger.LogError(ex, "Impossible de ping Redis au démarrage");
        }

        return connection;
    }
}