using System;
using System.Collections.Generic;
using System.Text;
using Ripple.EventTicketingSystem.Application.DTOs.Tickets;


namespace Ripple.EventTicketingSystem.Application.Interfaces
{

    public interface ITicketService
    {
        Task<TicketResponse> PurchaseAsync(
            Guid eventId,
            PurchaseTicketRequest request,
            CancellationToken cancellationToken);

        Task<EventAvailabilityResponse> GetAvailabilityAsync(
            Guid eventId,
            CancellationToken cancellationToken);
    }
}
