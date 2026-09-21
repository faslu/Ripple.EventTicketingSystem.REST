using System;
using System.Collections.Generic;
using System.Text;
using Ripple.EventTicketingSystem.Application.DTOs.Events;

namespace Ripple.EventTicketingSystem.Application.Interfaces
{
 

    public interface IEventService
    {
        Task<EventResponse> CreateAsync(
            CreateEventRequest request,
            CancellationToken cancellationToken);

        Task<List<EventResponse>> GetAllAsync(
            CancellationToken cancellationToken);

        Task<EventResponse> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<EventResponse> UpdateAsync(
            Guid id,
            UpdateEventRequest request,
            CancellationToken cancellationToken);

        Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken);
    }
}
