using Ripple.EventTicketingSystem.Domain.Models;

namespace Ripple.EventTicketingSystem.Application.Interfaces;

public interface ITicketRepository
{
    Task<bool> ExistsForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    void Add(Ticket ticket);
}