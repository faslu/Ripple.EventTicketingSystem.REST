using Microsoft.Extensions.Options;
using Ripple.EventTicketingSystem.Application.DTOs.Tickets;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Application.Options;
using Ripple.EventTicketingSystem.Domain.Exceptions;
using Ripple.EventTicketingSystem.Domain.Models;

namespace Ripple.EventTicketingSystem.Application.Services
{
    public class TicketService : ITicketService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly TicketingOptions _options;

        public TicketService(
            IUnitOfWork unitOfWork,
            IOptions<TicketingOptions> options)
        {
            _unitOfWork = unitOfWork;
            _options = options.Value;

        }

        public async Task<TicketResponse> PurchaseAsync(
            Guid eventId,
            PurchaseTicketRequest request,
            CancellationToken cancellationToken)
        {

            // Validate maximum tickets allowed per purchase
            if (request.Quantity > _options.MaxTicketsPerPurchase)
            {
                throw new ValidationException(
                    $"You can purchase a maximum of {_options.MaxTicketsPerPurchase} tickets per purchase.");
            }
            var eventExists = await _unitOfWork.Events
                .ExistsAsync(
                    eventId,
                    cancellationToken);

            if (!eventExists)
            {
                throw new NotFoundException(
                    $"Event '{eventId}' was not found.");
            }

            Ticket? ticket = null;
            PricingTier? tier = null;

            
                await _unitOfWork.ExecuteInTransactionAsync(
                    async ct =>
                    {
                        tier = await _unitOfWork.PricingTiers
                            .GetForEventAsync(
                                request.PricingTierId,
                                eventId,
                                ct);

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

                        ticket = new Ticket
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

                        _unitOfWork.Tickets.Add(ticket);

                        await _unitOfWork.SaveChangesAsync(ct);
                    },
                    cancellationToken);           
            

            if (ticket == null || tier == null)
            {
                throw new InvalidOperationException(
                    "Ticket purchase did not complete.");
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

                // TotalAmount is calculated by SQL Server.
                TotalAmount = ticket.Quantity * ticket.UnitPrice,

                PurchaseDate = ticket.PurchaseDate
            };
        }

        public async Task<EventAvailabilityResponse> GetAvailabilityAsync(
            Guid eventId,
            CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Events
                .GetByIdAsync(
                    eventId,
                    includePricingTiers: true,
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