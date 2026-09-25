using Ripple.EventTicketingSystem.Domain.Models;

namespace Ripple.EventTicketingSystem.Application.Interfaces;

public interface IPricingTierRepository
{
    Task<PricingTier?> GetForEventAsync(
        Guid pricingTierId,
        Guid eventId,
        CancellationToken cancellationToken = default);
}