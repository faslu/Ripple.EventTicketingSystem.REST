using Microsoft.VisualStudio.TestTools.UnitTesting;
using MockQueryable.Moq;
using Moq;
using Ripple.EventTicketingSystem.Application.DTOs.Events;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Application.Services;
using Ripple.EventTicketingSystem.Domain.Exceptions;
using Ripple.EventTicketingSystem.Domain.Models;

namespace Ripple.EventTicketingSystem.Application.Tests.Services;


[TestClass]
public class EventServiceTests
{
    [TestMethod]
    public async Task GetByIdAsync_WhenEventExists_ReturnsEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId,
                Name = "Sydney Tech Conference 2026",
                Description = "Technology conference in Sydney",
                Venue = "ICC Sydney",
                EventDate = new DateTime(2026, 10, 15),
                EventTime = new TimeSpan(9, 0, 0),
                TotalCapacity = 500,
                PricingTiers = new List<PricingTier>
                {
                    new PricingTier
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventId,
                        Name = "Early Bird",
                        Price = 149.00m,
                        Capacity = 100,
                        AvailableQuantity = 80
                    }
                    ,new PricingTier
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventId,
                        Name = "Standard",
                        Price = 199.00m,
                        Capacity = 300,
                        AvailableQuantity = 250
                    }
                }
            }
        };

        var mockEvents = events
            //.AsQueryable()
            .BuildMockDbSet<Event>();

        var mockDbContext = new Mock<ITicketingDbContext>();

        mockDbContext
            .Setup(x => x.Events)
            .Returns(mockEvents.Object);

        var service = new EventService(mockDbContext.Object);

        // Act
        var result = await service.GetByIdAsync(
            eventId,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(eventId, result.Id);
        Assert.AreEqual("Sydney Tech Conference 2026", result.Name);
        Assert.AreEqual("Technology conference in Sydney", result.Description);
        Assert.AreEqual("ICC Sydney", result.Venue);
        Assert.AreEqual(new DateTime(2026, 10, 15), result.EventDate);
        Assert.AreEqual(new TimeSpan(9, 0, 0), result.EventTime);
        Assert.AreEqual(500, result.TotalCapacity);

        Assert.IsNotNull(result.PricingTiers);
        Assert.HasCount(2, result.PricingTiers);

        Assert.AreEqual("Early Bird", result.PricingTiers[0].Name);
        Assert.AreEqual(149.00m, result.PricingTiers[0].Price);
        Assert.AreEqual(80, result.PricingTiers[0].AvailableQuantity);
    }

    [TestMethod]
    public async Task CreateAsync_WhenEventDateIsPast_ThrowsConflictException()
    {
        // Arrange
        var mockDb = new Mock<ITicketingDbContext>();
        var service = new EventService(mockDb.Object);

        var request = new CreateEventRequest
        {
            Name = "Old Event",
            Description = "Past event",
            Venue = "Sydney",
            EventDate = DateTime.UtcNow.AddDays(-1),
            EventTime = new TimeSpan(10, 0, 0),
            TotalCapacity = 100,
            PricingTiers = new List<CreatePricingTierRequest>
        {
            new CreatePricingTierRequest
            {
                Name = "Standard",
                Price = 50,
                Capacity = 100
            }
        }
        };

        // Act & Assert
        await Assert.ThrowsExactlyAsync<ConflictException>(
            () => service.CreateAsync(request, CancellationToken.None));
    }


    [TestMethod]
    public async Task CreateAsync_WhenDuplicateTierNames_ThrowsConflictException()
    {
        var mockDb = new Mock<ITicketingDbContext>();
        mockDb.Setup(x => x.Events).Returns(new List<Event>().BuildMockDbSet().Object);

        var service = new EventService(mockDb.Object);

        var request = new CreateEventRequest
        {
            Name = "Music Fest",
            Description = "Annual festival",
            Venue = "Sydney Arena",
            EventDate = DateTime.UtcNow.AddDays(5),
            EventTime = new TimeSpan(18, 0, 0),
            TotalCapacity = 200,
            PricingTiers = new List<CreatePricingTierRequest>
        {
            new CreatePricingTierRequest { Name = "VIP", Price = 300, Capacity = 100 },
            new CreatePricingTierRequest { Name = "vip", Price = 250, Capacity = 100 }
        }
        };

        await Assert.ThrowsExactlyAsync<ConflictException>(() =>
            service.CreateAsync(request, CancellationToken.None));
    }
    [TestMethod]
    public async Task CreateAsync_WhenValid_ReturnsEventResponse()
    {
        var mockDb = new Mock<ITicketingDbContext>();

        var events = new List<Event>().BuildMockDbSet();
        mockDb.Setup(x => x.Events).Returns(events.Object);

        mockDb.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(1);

        var service = new EventService(mockDb.Object);

        var request = new CreateEventRequest
        {
            Name = "Tech Expo",
            Description = "Technology Exhibition",
            Venue = "ICC Sydney",
            EventDate = DateTime.UtcNow.AddDays(10),
            EventTime = new TimeSpan(9, 0, 0),
            TotalCapacity = 300,
            PricingTiers = new List<CreatePricingTierRequest>
        {
            new CreatePricingTierRequest { Name = "Standard", Price = 100, Capacity = 200 },
            new CreatePricingTierRequest { Name = "VIP", Price = 300, Capacity = 100 }
        }
        };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual("Tech Expo", result.Name);
        Assert.AreEqual(300, result.TotalCapacity);
        Assert.HasCount(2, result.PricingTiers);
    }

    [TestMethod]
    public async Task GetAllAsync_WhenEventsExist_ReturnsOrderedList()
    {
        var events = new List<Event>
    {
        new Event { Id = Guid.NewGuid(), Name = "B Event", EventDate = new DateTime(2026, 10, 20), EventTime = new TimeSpan(10, 0, 0) },
        new Event { Id = Guid.NewGuid(), Name = "A Event", EventDate = new DateTime(2026, 10, 15), EventTime = new TimeSpan(9, 0, 0) }
    };

        var mockDb = new Mock<ITicketingDbContext>();
        mockDb.Setup(x => x.Events).Returns(events.BuildMockDbSet().Object);

        var service = new EventService(mockDb.Object);

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.HasCount(2, result);
        Assert.AreEqual("A Event", result.First().Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenEventNotFound_ThrowsNotFoundException()
    {
        var mockDb = new Mock<ITicketingDbContext>();
        mockDb.Setup(x => x.Events).Returns(new List<Event>().BuildMockDbSet().Object);

        var service = new EventService(mockDb.Object);

        await Assert.ThrowsExactlyAsync<NotFoundException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [TestMethod]
    public async Task UpdateAsync_WhenEventDateIsPast_ThrowsConflictException()
    {
        var eventId = Guid.NewGuid();

        var events = new List<Event>
    {
        new Event
        {
            Id = eventId,
            PricingTiers = new List<PricingTier>
            {
                new PricingTier { Capacity = 100 }
            }
        }
    }.BuildMockDbSet();

        var mockDb = new Mock<ITicketingDbContext>();
        mockDb.Setup(x => x.Events).Returns(events.Object);

        var service = new EventService(mockDb.Object);

        var request = new UpdateEventRequest
        {
            Name = "Updated",
            Description = "Updated",
            Venue = "Updated",
            EventDate = DateTime.UtcNow.AddDays(-1),
            EventTime = new TimeSpan(10, 0, 0),
            TotalCapacity = 200
        };

        await Assert.ThrowsExactlyAsync<ConflictException>(() =>
            service.UpdateAsync(eventId, request, CancellationToken.None));
    }

    [TestMethod]
    public async Task UpdateAsync_WhenTotalCapacityLessThanAllocated_ThrowsConflictException()
    {
        var eventId = Guid.NewGuid();

        var events = new List<Event>
    {
        new Event
        {
            Id = eventId,
            PricingTiers = new List<PricingTier>
            {
                new PricingTier { Capacity = 150 }
            }
        }
    }.BuildMockDbSet();

        var mockDb = new Mock<ITicketingDbContext>();
        mockDb.Setup(x => x.Events).Returns(events.Object);

        var service = new EventService(mockDb.Object);

        var request = new UpdateEventRequest
        {
            Name = "Updated",
            Description = "Updated",
            Venue = "Updated",
            EventDate = DateTime.UtcNow.AddDays(5),
            EventTime = new TimeSpan(10, 0, 0),
            TotalCapacity = 100 // Less than allocated 150
        };

        await Assert.ThrowsExactlyAsync<ConflictException>(() =>
            service.UpdateAsync(eventId, request, CancellationToken.None));
    }

    [TestMethod]
    public async Task UpdateAsync_WhenValid_UpdatesEvent()
    {
        var eventId = Guid.NewGuid();

        var events = new List<Event>
    {
        new Event
        {
            Id = eventId,
            Name = "Old Name",
            Venue = "Old Venue",
            PricingTiers = new List<PricingTier>
            {
                new PricingTier { Capacity = 100 }
            }
        }
    }.BuildMockDbSet();

        var mockDb = new Mock<ITicketingDbContext>();
        mockDb.Setup(x => x.Events).Returns(events.Object);

        mockDb.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(1);

        var service = new EventService(mockDb.Object);

        var request = new UpdateEventRequest
        {
            Name = "New Name",
            Description = "New Desc",
            Venue = "New Venue",
            EventDate = DateTime.UtcNow.AddDays(5),
            EventTime = new TimeSpan(10, 0, 0),
            TotalCapacity = 200
        };

        var result = await service.UpdateAsync(eventId, request, CancellationToken.None);

        Assert.AreEqual("New Name", result.Name);
        Assert.AreEqual("New Venue", result.Venue);
    }

    [TestMethod]
    public async Task DeleteAsync_WhenEventNotFound_ThrowsNotFoundException()
    {
        var mockDb = new Mock<ITicketingDbContext>();
        mockDb.Setup(x => x.Events).Returns(new List<Event>().BuildMockDbSet().Object);

        var service = new EventService(mockDb.Object);

        await Assert.ThrowsExactlyAsync<NotFoundException>(() =>
            service.DeleteAsync(Guid.NewGuid(), CancellationToken.None));
    }
    [TestMethod]
    public async Task DeleteAsync_WhenValid_DeletesEvent()
    {
        var eventId = Guid.NewGuid();

        var events = new List<Event>
    {
        new Event { Id = eventId }
    }.BuildMockDbSet();

        var tickets = new List<Ticket>().BuildMockDbSet(); // No tickets

        var mockDb = new Mock<ITicketingDbContext>();
        mockDb.Setup(x => x.Events).Returns(events.Object);
        mockDb.Setup(x => x.Tickets).Returns(tickets.Object);

        mockDb.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(1);

        var service = new EventService(mockDb.Object);

        await service.DeleteAsync(eventId, CancellationToken.None);

        mockDb.Verify(x => x.Events.Remove(It.Is<Event>(e => e.Id == eventId)), Times.Once);
    }



}