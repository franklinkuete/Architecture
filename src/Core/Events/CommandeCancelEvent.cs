namespace Core.Events;

public record CommandeCancelEvent(List<ProductStock> products,int? CommandeId);

