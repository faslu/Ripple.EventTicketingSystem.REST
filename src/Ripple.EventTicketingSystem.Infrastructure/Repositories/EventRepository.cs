using Microsoft.EntityFrameworkCore;
using Ripple.EventTicketingSystem.Application.DTOs.Reports;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Domain.Models;
using Ripple.EventTicketingSystem.Infrastructure.Data;

namespace Ripple.EventTicketingSystem.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly TicketingDbContext _db;

    public EventRepository(TicketingDbContext db)
    {
        _db = db;
    }

    public async Task<Event?> GetByIdAsync(
        Guid id,
        bool includePricingTiers = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Event> query = _db.Events;

        if (includePricingTiers)
        {
            query = query.Include(x => x.PricingTiers);
        }

        return await query.FirstOrDefaultAsync(
            x => x.Id == id,
            cancellationToken);
    }

    public async Task<List<Event>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Events
            .AsNoTracking()
            .Include(x => x.PricingTiers)
            .OrderBy(x => x.EventDate)
            .ThenBy(x => x.EventTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Events
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public void Add(Event entity)
    {
        _db.Events.Add(entity);
    }

    public void Remove(Event entity)
    {
        _db.Events.Remove(entity);
    }
    public async Task<List<SalesSummaryResponse>> GetSalesSummaryAsync(
    CancellationToken cancellationToken = default)
    {
        return await _db.Events
            .AsNoTracking()
            .Select(e => new SalesSummaryResponse
            {
                EventId = e.Id,
                EventName = e.Name,

                TicketsSold = e.Tickets
                    .Select(t => (int?)t.Quantity)
                    .Sum() ?? 0,

                Revenue = e.Tickets
                    .Select(t => (decimal?)t.TotalAmount)
                    .Sum() ?? 0
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(cancellationToken);
    }
}