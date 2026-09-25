using Microsoft.EntityFrameworkCore;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Domain.Models;
using Ripple.EventTicketingSystem.Infrastructure.Data;

namespace Ripple.EventTicketingSystem.Infrastructure.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly TicketingDbContext _db;

    public TicketRepository(TicketingDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExistsForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Tickets
            .AnyAsync(
                x => x.EventId == eventId,
                cancellationToken);
    }

    public void Add(Ticket ticket)
    {
        _db.Tickets.Add(ticket);
    }
}