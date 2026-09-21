using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ripple.EventTicketingSystem.Application.DTOs.Tickets;
using Ripple.EventTicketingSystem.Domain.Models;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Domain.Exceptions;


namespace Ripple.EventTicketingSystem.Application.Services
{

    public class TicketService : ITicketService
    {
        private readonly ITicketingDbContext _db;

        public TicketService(ITicketingDbContext db)
        {
            _db = db;
        }

        public async Task<TicketResponse> PurchaseAsync(
            Guid eventId,
            PurchaseTicketRequest request,
            CancellationToken cancellationToken)
        {
            var eventExists = await _db.Events
                .AnyAsync(
                    x => x.Id == eventId,
                    cancellationToken);

            if (!eventExists)
            {
                throw new NotFoundException(
                    $"Event '{eventId}' was not found.");
            }

            await using var transaction =
                await _db.BeginTransactionAsync(
                    cancellationToken);

            var tier = await _db.PricingTiers
                .FirstOrDefaultAsync(
                    x => x.Id == request.PricingTierId &&
                         x.EventId == eventId,
                    cancellationToken);

            if (tier == null)
            {
                throw new NotFoundException(
                    "Pricing tier was not found for this event.");
            }

            if (tier.AvailableQuantity < request.Quantity)
            {
                throw new ConflictException(
                    $"Only {tier.AvailableQuantity} tickets are available.");
            }

            tier.AvailableQuantity -= request.Quantity;

            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                PricingTierId = tier.Id,
                Quantity = request.Quantity,
                CustomerName = request.CustomerName.Trim(),
                CustomerEmail = request.CustomerEmail.Trim(),
                UnitPrice = tier.Price,
                PurchaseDate = DateTime.UtcNow
            };

            _db.Tickets.Add(ticket);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new ConflictException(
                    "The tickets were purchased by another customer. Please try again.");
            }

            return new TicketResponse
            {
                Id = ticket.Id,
                EventId = ticket.EventId,
                PricingTierId = ticket.PricingTierId,
                PricingTierName = tier.Name,
                Quantity = ticket.Quantity,
                CustomerName = ticket.CustomerName,
                CustomerEmail = ticket.CustomerEmail,
                UnitPrice = ticket.UnitPrice,
                TotalAmount = ticket.Quantity * ticket.UnitPrice,
                PurchaseDate = ticket.PurchaseDate
            };
        }

        public async Task<EventAvailabilityResponse> GetAvailabilityAsync(
            Guid eventId,
            CancellationToken cancellationToken)
        {
            var entity = await _db.Events
                .AsNoTracking()
                .Include(x => x.PricingTiers)
                .FirstOrDefaultAsync(
                    x => x.Id == eventId,
                    cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(
                    $"Event '{eventId}' was not found.");
            }

            return new EventAvailabilityResponse
            {
                EventId = entity.Id,
                EventName = entity.Name,
                TotalCapacity = entity.TotalCapacity,

                TotalAvailable = entity.PricingTiers
                    .Sum(x => x.AvailableQuantity),

                PricingTiers = entity.PricingTiers
                    .Select(x => new TierAvailabilityResponse
                    {
                        PricingTierId = x.Id,
                        Name = x.Name,
                        Price = x.Price,
                        Capacity = x.Capacity,
                        AvailableQuantity = x.AvailableQuantity
                    })
                    .ToList()
            };
        }
    }
}
