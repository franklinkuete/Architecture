
using Core.Events;

public record CommandeItemsAddedEvent(List<ProductStock> AddedProductList, int? CommandeId);
