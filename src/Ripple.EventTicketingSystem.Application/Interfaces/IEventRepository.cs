using Ripple.EventTicketingSystem.Application.DTOs.Reports;
using Ripple.EventTicketingSystem.Domain.Models;

namespace Ripple.EventTicketingSystem.Application.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(
        Guid id,
        bool includePricingTiers = false,
        CancellationToken cancellationToken = default);

    Task<List<Event>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    void Add(Event entity);

    void Remove(Event entity);
    Task<List<SalesSummaryResponse>> GetSalesSummaryAsync(
    CancellationToken cancellationToken = default);
}