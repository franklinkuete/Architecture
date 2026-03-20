namespace Core.Events;

// Cet événement informe le monde que le stock a échoué pour une commande précise
public record StockDecrementFailedEvent(int? CommandeId, string Reason,List<ProductStock> productToRetrieve);


