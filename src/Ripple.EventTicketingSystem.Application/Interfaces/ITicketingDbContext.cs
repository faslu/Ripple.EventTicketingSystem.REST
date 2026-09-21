using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Ripple.EventTicketingSystem.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;


namespace Ripple.EventTicketingSystem.Application.Interfaces
{
  

    public interface ITicketingDbContext
    {
        DbSet<Event> Events { get; }
        DbSet<PricingTier> PricingTiers { get; }
        DbSet<Ticket> Tickets { get; }
        //DatabaseFacade Database { get; }
        Task<IDbContextTransaction> BeginTransactionAsync(
           CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
