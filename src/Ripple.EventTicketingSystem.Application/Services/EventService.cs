using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ripple.EventTicketingSystem.Application.DTOs.Events;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Domain.Models;
using Ripple.EventTicketingSystem.Domain.Exceptions;

namespace Ripple.EventTicketingSystem.Application.Services
{


    public class EventService : IEventService
    {
        private readonly ITicketingDbContext _db;

        public EventService(ITicketingDbContext db)
        {
            _db = db;
        }

        public async Task<EventResponse> CreateAsync(
            CreateEventRequest request,
            CancellationToken cancellationToken)
        {
            if (request.EventDate.Date < DateTime.UtcNow.Date)
            {
                throw new ConflictException(
                    "Event date cannot be in the past.");
            }

            var pricingCapacity = request.PricingTiers
                .Sum(x => x.Capacity);

            if (pricingCapacity != request.TotalCapacity)
            {
                throw new ConflictException(
                    "The total pricing tier capacity must equal event capacity.");
            }

            var duplicateTierNames = request.PricingTiers
                .GroupBy(x => x.Name.Trim().ToLower())
                .Any(g => g.Count() > 1);

            if (duplicateTierNames)
            {
                throw new ConflictException(
                    "Pricing tier names must be unique.");
            }

            var entity = new Event
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Venue = request.Venue.Trim(),
                EventDate = request.EventDate.Date,
                EventTime = request.EventTime,
                TotalCapacity = request.TotalCapacity,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var tier in request.PricingTiers)
            {
                entity.PricingTiers.Add(new PricingTier
                {
                    Id = Guid.NewGuid(),
                    EventId = entity.Id,
                    Name = tier.Name.Trim(),
                    Price = tier.Price,
                    Capacity = tier.Capacity,
                    AvailableQuantity = tier.Capacity
                });
            }

            _db.Events.Add(entity);

            await _db.SaveChangesAsync(cancellationToken);

            return Map(entity);
        }

        public async Task<List<EventResponse>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            var events = await _db.Events
                .AsNoTracking()
                .Include(x => x.PricingTiers)
                .OrderBy(x => x.EventDate)
                .ThenBy(x => x.EventTime)
                .ToListAsync(cancellationToken);

            return events.Select(Map).ToList();
        }

        public async Task<EventResponse> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var entity = await _db.Events
                .AsNoTracking()
                .Include(x => x.PricingTiers)
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(
                    $"Event '{id}' was not found.");
            }

            return Map(entity);
        }

        public async Task<EventResponse> UpdateAsync(
            Guid id,
            UpdateEventRequest request,
            CancellationToken cancellationToken)
        {
            var entity = await _db.Events
                .Include(x => x.PricingTiers)
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(
                    $"Event '{id}' was not found.");
            }

            if (request.EventDate.Date < DateTime.UtcNow.Date)
            {
                throw new ConflictException(
                    "Event date cannot be in the past.");
            }

            var allocatedCapacity = entity.PricingTiers.Sum(x => x.Capacity);

            if (request.TotalCapacity < allocatedCapacity)
            {
                throw new ConflictException(
                    "Event capacity cannot be less than allocated pricing tier capacity.");
            }

            entity.Name = request.Name.Trim();
            entity.Description = request.Description?.Trim();
            entity.Venue = request.Venue.Trim();
            entity.EventDate = request.EventDate.Date;
            entity.EventTime = request.EventTime;
            entity.TotalCapacity = request.TotalCapacity;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return Map(entity);
        }

        public async Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var entity = await _db.Events
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(
                    $"Event '{id}' was not found.");
            }

            var hasTickets = await _db.Tickets
                .AnyAsync(
                    x => x.EventId == id,
                    cancellationToken);

            if (hasTickets)
            {
                throw new ConflictException(
                    "An event with ticket sales cannot be deleted.");
            }

            _db.Events.Remove(entity);

            await _db.SaveChangesAsync(cancellationToken);
        }

        private static EventResponse Map(Event entity)
        {
            return new EventResponse
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Venue = entity.Venue,
                EventDate = entity.EventDate,
                EventTime = entity.EventTime,
                TotalCapacity = entity.TotalCapacity,

                PricingTiers = entity.PricingTiers
                    .Select(t => new PricingTierResponse
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Price = t.Price,
                        Capacity = t.Capacity,
                        AvailableQuantity = t.AvailableQuantity
                    })
                    .ToList()
            };
        }
    }
}
