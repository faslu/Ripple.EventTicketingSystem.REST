using Microsoft.EntityFrameworkCore;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Domain.Exceptions;
using Ripple.EventTicketingSystem.Infrastructure.Data;

namespace Ripple.EventTicketingSystem.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly TicketingDbContext _db;

    public UnitOfWork(
        TicketingDbContext db,
        IEventRepository events,
        IPricingTierRepository pricingTiers,
        ITicketRepository tickets)
    {
        _db = db;

        Events = events;
        PricingTiers = pricingTiers;
        Tickets = tickets;
    }

    public IEventRepository Events { get; }

    public IPricingTierRepository PricingTiers { get; }

    public ITicketRepository Tickets { get; }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(
         Func<CancellationToken, Task> operation,
         CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            await operation(cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException) 
        { 
            await transaction.RollbackAsync(cancellationToken); 
            throw new ConflictException("The tickets were purchased by another customer. Please try again."); 
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }
}