namespace Core.Configuration;

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string Debug { get; set; } = string.Empty;
    public int MessageTimeoutMs { get; set; }
    public TransactionExchange KafkaTransaction { get; set; } = new TransactionExchange();
    public TransactionExchange MassTransitTransaction { get; set; } = new TransactionExchange();
}

public class TransactionExchange
{
    public string Name { get; set; } = string.Empty;
    public string ProducerTopic { get; set; } = string.Empty;
    public string ConsumerTopic { get; set; } = string.Empty;
}
