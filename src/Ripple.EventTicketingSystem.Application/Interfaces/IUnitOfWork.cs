
namespace Ripple.EventTicketingSystem.Application.Interfaces;

public interface IUnitOfWork
{
    IEventRepository Events { get; }

    IPricingTierRepository PricingTiers { get; }

    ITicketRepository Tickets { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);
}