using Microsoft.EntityFrameworkCore;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Domain.Models;
using Ripple.EventTicketingSystem.Infrastructure.Data;

namespace Ripple.EventTicketingSystem.Infrastructure.Repositories;

public class PricingTierRepository : IPricingTierRepository
{
    private readonly TicketingDbContext _db;

    public PricingTierRepository(TicketingDbContext db)
    {
        _db = db;
    }

    public async Task<PricingTier?> GetForEventAsync(
        Guid pricingTierId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await _db.PricingTiers
            .FirstOrDefaultAsync(
                x => x.Id == pricingTierId &&
                     x.EventId == eventId,
                cancellationToken);
    }
}