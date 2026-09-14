using ShopFlow.BuildingBlocks.Domain;

namespace ShopFlow.Domain.Identity.Events;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email) : DomainEvent;
