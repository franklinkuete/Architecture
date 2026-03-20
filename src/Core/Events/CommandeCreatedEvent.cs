namespace Core.Events;

public record CommandeCreatedEvent(List<ProductStock> products,int? CommandeId);

public record ProductStock(string ProductId, int Quantity);

