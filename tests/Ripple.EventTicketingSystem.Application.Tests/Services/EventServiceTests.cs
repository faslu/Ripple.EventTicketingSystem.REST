using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IEventRepository> _eventRepository = null!;
    private Mock<ITicketRepository> _ticketRepository = null!;
    private EventService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _eventRepository = new Mock<IEventRepository>();
        _ticketRepository = new Mock<ITicketRepository>();

        _unitOfWork
            .SetupGet(x => x.Events)
            .Returns(_eventRepository.Object);

        _unitOfWork
            .SetupGet(x => x.Tickets)
            .Returns(_ticketRepository.Object);

        _service = new EventService(_unitOfWork.Object);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenEventExists_ReturnsEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var entity = new Event
        {
            Id = eventId,
            Name = "Sydney Tech Conference 2026",
            Description = "Technology conference in Sydney",
            Venue = "ICC Sydney",
            EventDate = new DateTime(2026, 10, 15),
            EventTime = new TimeSpan(9, 0, 0),
            TotalCapacity = 500,
            PricingTiers =
            [
                new PricingTier
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    Name = "Early Bird",
                    Price = 149.00m,
                    Capacity = 100,
                    AvailableQuantity = 80
                },
                new PricingTier
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    Name = "Standard",
                    Price = 199.00m,
                    Capacity = 300,
                    AvailableQuantity = 250
                }
            ]
        };

        _eventRepository
            .Setup(x => x.GetByIdAsync(
                eventId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(
            eventId,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);

        Assert.AreEqual(eventId, result.Id);
        Assert.AreEqual(
            "Sydney Tech Conference 2026",
            result.Name);
        Assert.AreEqual(
            "Technology conference in Sydney",
            result.Description);
        Assert.AreEqual("ICC Sydney", result.Venue);
        Assert.AreEqual(
            new DateTime(2026, 10, 15),
            result.EventDate);
        Assert.AreEqual(
            new TimeSpan(9, 0, 0),
            result.EventTime);
        Assert.AreEqual(500, result.TotalCapacity);

        Assert.IsNotNull(result.PricingTiers);
        Assert.HasCount(2, result.PricingTiers);

        Assert.AreEqual(
            "Early Bird",
            result.PricingTiers[0].Name);
        Assert.AreEqual(
            149.00m,
            result.PricingTiers[0].Price);
        Assert.AreEqual(
            80,
            result.PricingTiers[0].AvailableQuantity);

        _eventRepository.Verify(
            x => x.GetByIdAsync(
                eventId,
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenEventNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _eventRepository
            .Setup(x => x.GetByIdAsync(
                eventId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<NotFoundException>(
            () => _service.GetByIdAsync(
                eventId,
                CancellationToken.None));
    }

    [TestMethod]
    public async Task GetAllAsync_WhenEventsExist_ReturnsOrderedList()
    {
        // Arrange
        var events = new List<Event>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Name = "B Event",
                EventDate = new DateTime(2026, 10, 20),
                EventTime = new TimeSpan(10, 0, 0)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Name = "A Event",
                EventDate = new DateTime(2026, 10, 15),
                EventTime = new TimeSpan(9, 0, 0)
            }
        };

        _eventRepository
            .Setup(x => x.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        // Act
        var result = await _service.GetAllAsync(
            CancellationToken.None);

        // Assert
        Assert.HasCount(2, result);

        // EventService currently does not perform ordering.
        // Repository is responsible for returning the required order.
        Assert.AreEqual("B Event", result[0].Name);
        Assert.AreEqual("A Event", result[1].Name);

        _eventRepository.Verify(
            x => x.GetAllAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task CreateAsync_WhenEventDateIsPast_ThrowsConflictException()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Name = "Old Event",
            Description = "Past event",
            Venue = "Sydney",
            EventDate = DateTime.UtcNow.AddDays(-1),
            EventTime = new TimeSpan(10, 0, 0),
            TotalCapacity = 100,
            PricingTiers =
            [
                new CreatePricingTierRequest
                {
                    Name = "Standard",
                    Price = 50,
                    Capacity = 100
                }
            ]
        };

        // Act & Assert
        await Assert.ThrowsExactlyAsync<ConflictException>(
            () => _service.CreateAsync(
                request,
                CancellationToken.None));

        _eventRepository.Verify(
            x => x.Add(It.IsAny<Event>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CreateAsync_WhenPricingCapacityDoesNotMatchEventCapacity_ThrowsConflictException()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Name = "Music Fest",
            Description = "Annual festival",
            Venue = "Sydney Arena",
            EventDate = DateTime.UtcNow.AddDays(5),
            EventTime = new TimeSpan(18, 0, 0),
            TotalCapacity = 200,
            PricingTiers =
            [
                new CreatePricingTierRequest
                {
                    Name = "Standard",
                    Price = 100,
                    Capacity = 100
                }
            ]
        };

        // Act & Assert
        await Assert.ThrowsExactlyAsync<ConflictException>(
            () => _service.CreateAsync(
                request,
                CancellationToken.None));

        _eventRepository.Verify(
            x => x.Add(It.IsAny<Event>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CreateAsync_WhenDuplicateTierNames_ThrowsConflictException()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Name = "Music Fest",
            Description = "Annual festival",
            Venue = "Sydney Arena",
            EventDate = DateTime.UtcNow.AddDays(5),
            EventTime = new TimeSpan(18, 0, 0),
            TotalCapacity = 200,
            PricingTiers =
            [
                new CreatePricingTierRequest
                {
                    Name = "VIP",
                    Price = 300,
                    Capacity = 100
                },
                new CreatePricingTierRequest
                {
                    Name = "vip",
                    Price = 250,
                    Capacity = 100
                }
            ]
        };

        // Act & Assert
        await Assert.ThrowsExactlyAsync<ConflictException>(
            () => _service.CreateAsync(
                request,
                CancellationToken.None));

        _eventRepository.Verify(
            x => x.Add(It.IsAny<Event>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CreateAsync_WhenValid_ReturnsEventResponse()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Name = " Tech Expo ",
            Description = " Technology Exhibition ",
            Venue = " ICC Sydney ",
            EventDate = DateTime.UtcNow.AddDays(10),
            EventTime = new TimeSpan(9, 0, 0),
            TotalCapacity = 300,
            PricingTiers =
            [
                new CreatePricingTierRequest
                {
                    Name = " Standard ",
                    Price = 100,
                    Capacity = 200
                },
                new CreatePricingTierRequest
                {
                    Name = " VIP ",
                    Price = 300,
                    Capacity = 100
                }
            ]
        };

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateAsync(
            request,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);

        Assert.AreEqual("Tech Expo", result.Name);
        Assert.AreEqual(
            "Technology Exhibition",
            result.Description);
        Assert.AreEqual("ICC Sydney", result.Venue);
        Assert.AreEqual(300, result.TotalCapacity);

        Assert.HasCount(2, result.PricingTiers);

        Assert.AreEqual(
            "Standard",
            result.PricingTiers[0].Name);
        Assert.AreEqual(
            100m,
            result.PricingTiers[0].Price);
        Assert.AreEqual(
            200,
            result.PricingTiers[0].Capacity);
        Assert.AreEqual(
            200,
            result.PricingTiers[0].AvailableQuantity);

        Assert.AreEqual(
            "VIP",
            result.PricingTiers[1].Name);
        Assert.AreEqual(
            300m,
            result.PricingTiers[1].Price);
        Assert.AreEqual(
            100,
            result.PricingTiers[1].Capacity);
        Assert.AreEqual(
            100,
            result.PricingTiers[1].AvailableQuantity);

        _eventRepository.Verify(
            x => x.Add(It.Is<Event>(e =>
                e.Name == "Tech Expo" &&
                e.Venue == "ICC Sydney" &&
                e.TotalCapacity == 300 &&
                e.PricingTiers.Count == 2)),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task UpdateAsync_WhenEventNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _eventRepository
            .Setup(x => x.GetByIdAsync(
                eventId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var request = CreateValidUpdateRequest();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<NotFoundException>(
            () => _service.UpdateAsync(
                eventId,
                request,
                CancellationToken.None));

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task UpdateAsync_WhenEventDateIsPast_ThrowsConflictException()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var entity = new Event
        {
            Id = eventId,
            PricingTiers =
            [
                new PricingTier
                {
                    Capacity = 100
                }
            ]
        };

        _eventRepository
            .Setup(x => x.GetByIdAsync(
                eventId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var request = new UpdateEventRequest
        {
            Name = "Updated",
            Description = "Updated",
            Venue = "Updated",
            EventDate = DateTime.UtcNow.AddDays(-1),
            EventTime = new TimeSpan(10, 0, 0),
            TotalCapacity = 200
        };

        // Act & Assert
        await Assert.ThrowsExactlyAsync<ConflictException>(
            () => _service.UpdateAsync(
                eventId,
                request,
                CancellationToken.None));

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task UpdateAsync_WhenTotalCapacityLessThanAllocated_ThrowsConflictException()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var entity = new Event
        {
            Id = eventId,
            PricingTiers =
            [
                new PricingTier
                {
                    Capacity = 150
                }
            ]
        };

        _eventRepository
            .Setup(x => x.GetByIdAsync(
                eventId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var request = new UpdateEventRequest
        {
            Name = "Updated",
            Description = "Updated",
            Venue = "Updated",
            EventDate = DateTime.UtcNow.AddDays(5),
            EventTime = new TimeSpan(10, 0, 0),
            TotalCapacity = 100
        };

        // Act & Assert
        await Assert.ThrowsExactlyAsync<ConflictException>(
            () => _service.UpdateAsync(
                eventId,
                request,
                CancellationToken.None));

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task UpdateAsync_WhenValid_UpdatesEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var entity = new Event
        {
            Id = eventId,
            Name = "Old Name",
            Description = "Old Description",
            Venue = "Old Venue",
            EventDate = DateTime.UtcNow.AddDays(10),
            EventTime = new TimeSpan(10, 0, 0),
            TotalCapacity = 100,
            PricingTiers =
            [
                new PricingTier
                {
                    Capacity = 100
                }
            ]
        };

        _eventRepository
            .Setup(x => x.GetByIdAsync(
                eventId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new UpdateEventRequest
        {
            Name = "New Name",
            Description = "New Desc",
            Venue = "New Venue",
            EventDate = DateTime.UtcNow.AddDays(5),
            EventTime = new TimeSpan(14, 0, 0),
            TotalCapacity = 200
        };

        // Act
        var result = await _service.UpdateAsync(
            eventId,
            request,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);

        Assert.AreEqual("New Name", result.Name);
        Assert.AreEqual("New Desc", result.Description);
        Assert.AreEqual("New Venue", result.Venue);
        Assert.AreEqual(200, result.TotalCapacity);
        Assert.AreEqual(
            new TimeSpan(14, 0, 0),
            result.EventTime);

        Assert.IsNotNull(entity.UpdatedAt);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task DeleteAsync_WhenEventNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _eventRepository
            .Setup(x => x.GetByIdAsync(
                eventId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<NotFoundException>(
            () => _service.DeleteAsync(
                eventId,
                CancellationToken.None));

        _ticketRepository.Verify(
            x => x.ExistsForEventAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task DeleteAsync_WhenEventHasTickets_ThrowsConflictException()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var entity = new Event
        {
            Id = eventId,
            Name = "Sold Event"
        };

        _eventRepository
            .Setup(x => x.GetByIdAsync(
                eventId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _ticketRepository
            .Setup(x => x.ExistsForEventAsync(
                eventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<ConflictException>(
            () => _service.DeleteAsync(
                eventId,
                CancellationToken.None));

        _eventRepository.Verify(
            x => x.Remove(It.IsAny<Event>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task DeleteAsync_WhenValid_DeletesEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var entity = new Event
        {
            Id = eventId,
            Name = "Event To Delete"
        };

        _eventRepository
            .Setup(x => x.GetByIdAsync(
                eventId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _ticketRepository
            .Setup(x => x.ExistsForEventAsync(
                eventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.DeleteAsync(
            eventId,
            CancellationToken.None);

        // Assert
        _eventRepository.Verify(
            x => x.Remove(entity),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static UpdateEventRequest CreateValidUpdateRequest()
    {
        return new UpdateEventRequest
        {
            Name = "Updated Event",
            Description = "Updated Description",
            Venue = "Updated Venue",
            EventDate = DateTime.UtcNow.AddDays(5),
            EventTime = new TimeSpan(10, 0, 0),
            TotalCapacity = 200
        };
    }
}

