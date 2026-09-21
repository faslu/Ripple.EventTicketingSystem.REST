using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MockQueryable.Moq;
using Moq;
using Ripple.EventTicketingSystem.Application.DTOs.Tickets;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Application.Services;
using Ripple.EventTicketingSystem.Domain.Exceptions;
using Ripple.EventTicketingSystem.Domain.Models;

namespace Ripple.EventTicketingSystem.Application.Tests.Services;

[TestClass]
public class TicketServiceTests
{
    [TestMethod]
    public async Task PurchaseAsync_WhenEventDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(new List<Event>()
                .BuildMockDbSet()
                .Object);

        var service = new TicketService(mockDb.Object);

        var request = new PurchaseTicketRequest
        {
            PricingTierId = Guid.NewGuid(),
            Quantity = 1,
            CustomerName = "John",
            CustomerEmail = "john@example.com"
        };

        // Act & Assert
        await Assert.ThrowsExactlyAsync<NotFoundException>(
            () => service.PurchaseAsync(
                Guid.NewGuid(),
                request,
                CancellationToken.None));
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenPricingTierDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId,
                Name = "Music Festival"
            }
        }.BuildMockDbSet();

        var pricingTiers = new List<PricingTier>()
            .BuildMockDbSet();

        var transaction = new Mock<IDbContextTransaction>();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        mockDb.Setup(x => x.PricingTiers)
            .Returns(pricingTiers.Object);

        mockDb.Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var service = new TicketService(mockDb.Object);

        var request = new PurchaseTicketRequest
        {
            PricingTierId = Guid.NewGuid(),
            Quantity = 1,
            CustomerName = "John",
            CustomerEmail = "john@example.com"
        };

        // Act & Assert
        await Assert.ThrowsExactlyAsync<NotFoundException>(
            () => service.PurchaseAsync(
                eventId,
                request,
                CancellationToken.None));

        transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenPricingTierBelongsToAnotherEvent_ThrowsNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var anotherEventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId,
                Name = "Music Festival"
            }
        }.BuildMockDbSet();

        var pricingTiers = new List<PricingTier>
        {
            new PricingTier
            {
                Id = tierId,
                EventId = anotherEventId,
                Name = "Standard",
                Price = 100m,
                Capacity = 100,
                AvailableQuantity = 50
            }
        }.BuildMockDbSet();

        var transaction = new Mock<IDbContextTransaction>();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        mockDb.Setup(x => x.PricingTiers)
            .Returns(pricingTiers.Object);

        mockDb.Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var service = new TicketService(mockDb.Object);

        var request = new PurchaseTicketRequest
        {
            PricingTierId = tierId,
            Quantity = 1,
            CustomerName = "John",
            CustomerEmail = "john@example.com"
        };

        // Act & Assert
        await Assert.ThrowsExactlyAsync<NotFoundException>(
            () => service.PurchaseAsync(
                eventId,
                request,
                CancellationToken.None));

        transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenInsufficientQuantity_ThrowsConflictException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId,
                Name = "Music Festival"
            }
        }.BuildMockDbSet();

        var pricingTiers = new List<PricingTier>
        {
            new PricingTier
            {
                Id = tierId,
                EventId = eventId,
                Name = "VIP",
                Price = 200m,
                Capacity = 100,
                AvailableQuantity = 2
            }
        }.BuildMockDbSet();

        var transaction = new Mock<IDbContextTransaction>();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        mockDb.Setup(x => x.PricingTiers)
            .Returns(pricingTiers.Object);

        mockDb.Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var service = new TicketService(mockDb.Object);

        var request = new PurchaseTicketRequest
        {
            PricingTierId = tierId,
            Quantity = 5,
            CustomerName = "John",
            CustomerEmail = "john@example.com"
        };

        // Act & Assert
        await Assert.ThrowsExactlyAsync<ConflictException>(
            () => service.PurchaseAsync(
                eventId,
                request,
                CancellationToken.None));

        transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenExactAvailableQuantityIsRequested_Succeeds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId,
                Name = "Music Festival"
            }
        }.BuildMockDbSet();

        var pricingTiers = new List<PricingTier>
        {
            new PricingTier
            {
                Id = tierId,
                EventId = eventId,
                Name = "Standard",
                Price = 100m,
                Capacity = 10,
                AvailableQuantity = 2
            }
        }.BuildMockDbSet();

        var tickets = new List<Ticket>()
            .BuildMockDbSet();

        var transaction = new Mock<IDbContextTransaction>();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        mockDb.Setup(x => x.PricingTiers)
            .Returns(pricingTiers.Object);

        mockDb.Setup(x => x.Tickets)
            .Returns(tickets.Object);

        mockDb.Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        mockDb.Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new TicketService(mockDb.Object);

        var request = new PurchaseTicketRequest
        {
            PricingTierId = tierId,
            Quantity = 2,
            CustomerName = "John",
            CustomerEmail = "john@example.com"
        };

        // Act
        var result = await service.PurchaseAsync(
            eventId,
            request,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Quantity);
        Assert.AreEqual(0, pricingTiers.Object.First().AvailableQuantity);

        transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenValid_ReturnsCorrectTicketResponse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId,
                Name = "Music Festival"
            }
        }.BuildMockDbSet();

        var pricingTiers = new List<PricingTier>
        {
            new PricingTier
            {
                Id = tierId,
                EventId = eventId,
                Name = "Standard",
                Price = 100m,
                Capacity = 100,
                AvailableQuantity = 10
            }
        }.BuildMockDbSet();

        var tickets = new List<Ticket>()
            .BuildMockDbSet();

        var transaction = new Mock<IDbContextTransaction>();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        mockDb.Setup(x => x.PricingTiers)
            .Returns(pricingTiers.Object);

        mockDb.Setup(x => x.Tickets)
            .Returns(tickets.Object);

        mockDb.Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        mockDb.Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new TicketService(mockDb.Object);

        var request = new PurchaseTicketRequest
        {
            PricingTierId = tierId,
            Quantity = 2,
            CustomerName = "John",
            CustomerEmail = "john@example.com"
        };

        // Act
        var result = await service.PurchaseAsync(
            eventId,
            request,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);

        Assert.AreEqual(eventId, result.EventId);
        Assert.AreEqual(tierId, result.PricingTierId);
        Assert.AreEqual("Standard", result.PricingTierName);
        Assert.AreEqual(2, result.Quantity);
        Assert.AreEqual("John", result.CustomerName);
        Assert.AreEqual("john@example.com", result.CustomerEmail);
        Assert.AreEqual(100m, result.UnitPrice);
        Assert.AreEqual(200m, result.TotalAmount);

        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreNotEqual(default, result.PurchaseDate);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenCustomerDetailsContainWhitespace_TrimsValues()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId
            }
        }.BuildMockDbSet();

        var pricingTiers = new List<PricingTier>
        {
            new PricingTier
            {
                Id = tierId,
                EventId = eventId,
                Name = "Standard",
                Price = 75m,
                Capacity = 50,
                AvailableQuantity = 10
            }
        }.BuildMockDbSet();

        var tickets = new List<Ticket>()
            .BuildMockDbSet();

        var transaction = new Mock<IDbContextTransaction>();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        mockDb.Setup(x => x.PricingTiers)
            .Returns(pricingTiers.Object);

        mockDb.Setup(x => x.Tickets)
            .Returns(tickets.Object);

        mockDb.Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        mockDb.Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new TicketService(mockDb.Object);

        var request = new PurchaseTicketRequest
        {
            PricingTierId = tierId,
            Quantity = 1,
            CustomerName = "  John Smith  ",
            CustomerEmail = "  john@example.com  "
        };

        // Act
        var result = await service.PurchaseAsync(
            eventId,
            request,
            CancellationToken.None);

        // Assert
        Assert.AreEqual("John Smith", result.CustomerName);
        Assert.AreEqual("john@example.com", result.CustomerEmail);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenValid_AddsTicketWithCorrectValues()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId
            }
        }.BuildMockDbSet();

        var pricingTiers = new List<PricingTier>
        {
            new PricingTier
            {
                Id = tierId,
                EventId = eventId,
                Name = "VIP",
                Price = 250m,
                Capacity = 100,
                AvailableQuantity = 20
            }
        }.BuildMockDbSet();

        var tickets = new List<Ticket>()
            .BuildMockDbSet();

        Ticket? addedTicket = null;

        tickets.Setup(x => x.Add(It.IsAny<Ticket>()))
            .Callback<Ticket>(ticket =>
            {
                addedTicket = ticket;
            });

        var transaction = new Mock<IDbContextTransaction>();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        mockDb.Setup(x => x.PricingTiers)
            .Returns(pricingTiers.Object);

        mockDb.Setup(x => x.Tickets)
            .Returns(tickets.Object);

        mockDb.Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        mockDb.Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new TicketService(mockDb.Object);

        var request = new PurchaseTicketRequest
        {
            PricingTierId = tierId,
            Quantity = 3,
            CustomerName = "Jane Smith",
            CustomerEmail = "jane@example.com"
        };

        // Act
        await service.PurchaseAsync(
            eventId,
            request,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(addedTicket);

        Assert.AreEqual(eventId, addedTicket.EventId);
        Assert.AreEqual(tierId, addedTicket.PricingTierId);
        Assert.AreEqual(3, addedTicket.Quantity);
        Assert.AreEqual("Jane Smith", addedTicket.CustomerName);
        Assert.AreEqual("jane@example.com", addedTicket.CustomerEmail);
        Assert.AreEqual(250m, addedTicket.UnitPrice);

        tickets.Verify(
            x => x.Add(It.IsAny<Ticket>()),
            Times.Once);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenValid_ReducesAvailableQuantity()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var tier = new PricingTier
        {
            Id = tierId,
            EventId = eventId,
            Name = "Standard",
            Price = 100m,
            Capacity = 100,
            AvailableQuantity = 10
        };

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId
            }
        }.BuildMockDbSet();

        var pricingTiers = new List<PricingTier>
        {
            tier
        }.BuildMockDbSet();

        var tickets = new List<Ticket>()
            .BuildMockDbSet();

        var transaction = new Mock<IDbContextTransaction>();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        mockDb.Setup(x => x.PricingTiers)
            .Returns(pricingTiers.Object);

        mockDb.Setup(x => x.Tickets)
            .Returns(tickets.Object);

        mockDb.Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        mockDb.Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new TicketService(mockDb.Object);

        var request = new PurchaseTicketRequest
        {
            PricingTierId = tierId,
            Quantity = 4,
            CustomerName = "John",
            CustomerEmail = "john@example.com"
        };

        // Act
        await service.PurchaseAsync(
            eventId,
            request,
            CancellationToken.None);

        // Assert
        Assert.AreEqual(6, tier.AvailableQuantity);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenValid_CallsSaveChangesOnce()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId
            }
        }.BuildMockDbSet();

        var pricingTiers = new List<PricingTier>
        {
            new PricingTier
            {
                Id = tierId,
                EventId = eventId,
                Name = "Standard",
                Price = 100m,
                Capacity = 100,
                AvailableQuantity = 10
            }
        }.BuildMockDbSet();

        var tickets = new List<Ticket>()
            .BuildMockDbSet();

        var transaction = new Mock<IDbContextTransaction>();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        mockDb.Setup(x => x.PricingTiers)
            .Returns(pricingTiers.Object);

        mockDb.Setup(x => x.Tickets)
            .Returns(tickets.Object);

        mockDb.Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        mockDb.Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new TicketService(mockDb.Object);

        var request = new PurchaseTicketRequest
        {
            PricingTierId = tierId,
            Quantity = 1,
            CustomerName = "John",
            CustomerEmail = "john@example.com"
        };

        // Act
        await service.PurchaseAsync(
            eventId,
            request,
            CancellationToken.None);

        // Assert
        mockDb.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenValid_CommitsTransaction()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId
            }
        }.BuildMockDbSet();

        var pricingTiers = new List<PricingTier>
        {
            new PricingTier
            {
                Id = tierId,
                EventId = eventId,
                Name = "Standard",
                Price = 100m,
                Capacity = 100,
                AvailableQuantity = 10
            }
        }.BuildMockDbSet();

        var tickets = new List<Ticket>()
            .BuildMockDbSet();

        var transaction = new Mock<IDbContextTransaction>();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        mockDb.Setup(x => x.PricingTiers)
            .Returns(pricingTiers.Object);

        mockDb.Setup(x => x.Tickets)
            .Returns(tickets.Object);

        mockDb.Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        mockDb.Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new TicketService(mockDb.Object);

        var request = new PurchaseTicketRequest
        {
            PricingTierId = tierId,
            Quantity = 2,
            CustomerName = "John",
            CustomerEmail = "john@example.com"
        };

        // Act
        await service.PurchaseAsync(
            eventId,
            request,
            CancellationToken.None);

        // Assert
        transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        transaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenConcurrencyExceptionOccurs_ThrowsConflictException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId
            }
        }.BuildMockDbSet();

        var pricingTiers = new List<PricingTier>
        {
            new PricingTier
            {
                Id = tierId,
                EventId = eventId,
                Name = "Standard",
                Price = 100m,
                Capacity = 100,
                AvailableQuantity = 10
            }
        }.BuildMockDbSet();

        var tickets = new List<Ticket>()
            .BuildMockDbSet();

        var transaction = new Mock<IDbContextTransaction>();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        mockDb.Setup(x => x.PricingTiers)
            .Returns(pricingTiers.Object);

        mockDb.Setup(x => x.Tickets)
            .Returns(tickets.Object);

        mockDb.Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        // The concurrency exception should happen here.
        mockDb.Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var service = new TicketService(mockDb.Object);

        var request = new PurchaseTicketRequest
        {
            PricingTierId = tierId,
            Quantity = 1,
            CustomerName = "John",
            CustomerEmail = "john@example.com"
        };

        // Act & Assert
        await Assert.ThrowsExactlyAsync<ConflictException>(
            () => service.PurchaseAsync(
                eventId,
                request,
                CancellationToken.None));

        transaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenConcurrencyExceptionOccurs_ReturnsExpectedConflictMessage()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId
            }
        }.BuildMockDbSet();

        var pricingTiers = new List<PricingTier>
        {
            new PricingTier
            {
                Id = tierId,
                EventId = eventId,
                Name = "Standard",
                Price = 100m,
                Capacity = 100,
                AvailableQuantity = 10
            }
        }.BuildMockDbSet();

        var tickets = new List<Ticket>()
            .BuildMockDbSet();

        var transaction = new Mock<IDbContextTransaction>();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        mockDb.Setup(x => x.PricingTiers)
            .Returns(pricingTiers.Object);

        mockDb.Setup(x => x.Tickets)
            .Returns(tickets.Object);

        mockDb.Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        mockDb.Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var service = new TicketService(mockDb.Object);

        var request = new PurchaseTicketRequest
        {
            PricingTierId = tierId,
            Quantity = 1,
            CustomerName = "John",
            CustomerEmail = "john@example.com"
        };

        // Act
        var exception = await Assert.ThrowsExactlyAsync<ConflictException>(
            () => service.PurchaseAsync(
                eventId,
                request,
                CancellationToken.None));

        // Assert
        Assert.AreEqual(
            "The tickets were purchased by another customer. Please try again.",
            exception.Message);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_WhenEventDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(new List<Event>()
                .BuildMockDbSet()
                .Object);

        var service = new TicketService(mockDb.Object);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<NotFoundException>(
            () => service.GetAvailabilityAsync(
                Guid.NewGuid(),
                CancellationToken.None));
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_WhenValid_ReturnsEventDetails()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId,
                Name = "Tech Expo",
                TotalCapacity = 300,
                PricingTiers = new List<PricingTier>
                {
                    new PricingTier
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventId,
                        Name = "VIP",
                        Price = 300m,
                        Capacity = 100,
                        AvailableQuantity = 20
                    },
                    new PricingTier
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventId,
                        Name = "Standard",
                        Price = 100m,
                        Capacity = 200,
                        AvailableQuantity = 150
                    }
                }
            }
        }.BuildMockDbSet();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        var service = new TicketService(mockDb.Object);

        // Act
        var result = await service.GetAvailabilityAsync(
            eventId,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(eventId, result.EventId);
        Assert.AreEqual("Tech Expo", result.EventName);
        Assert.AreEqual(300, result.TotalCapacity);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_WhenValid_ComputesTotalAvailableQuantity()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId,
                Name = "Tech Expo",
                TotalCapacity = 300,
                PricingTiers = new List<PricingTier>
                {
                    new PricingTier
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventId,
                        Name = "VIP",
                        Price = 300m,
                        Capacity = 100,
                        AvailableQuantity = 20
                    },
                    new PricingTier
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventId,
                        Name = "Standard",
                        Price = 100m,
                        Capacity = 200,
                        AvailableQuantity = 150
                    }
                }
            }
        }.BuildMockDbSet();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        var service = new TicketService(mockDb.Object);

        // Act
        var result = await service.GetAvailabilityAsync(
            eventId,
            CancellationToken.None);

        // Assert
        Assert.AreEqual(170, result.TotalAvailable);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_WhenValid_ReturnsAllPricingTiers()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var vipTierId = Guid.NewGuid();
        var standardTierId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId,
                Name = "Tech Expo",
                TotalCapacity = 300,
                PricingTiers = new List<PricingTier>
                {
                    new PricingTier
                    {
                        Id = vipTierId,
                        EventId = eventId,
                        Name = "VIP",
                        Price = 300m,
                        Capacity = 100,
                        AvailableQuantity = 20
                    },
                    new PricingTier
                    {
                        Id = standardTierId,
                        EventId = eventId,
                        Name = "Standard",
                        Price = 100m,
                        Capacity = 200,
                        AvailableQuantity = 150
                    }
                }
            }
        }.BuildMockDbSet();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        var service = new TicketService(mockDb.Object);

        // Act
        var result = await service.GetAvailabilityAsync(
            eventId,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result.PricingTiers);
        Assert.HasCount(2, result.PricingTiers);

        var vip = result.PricingTiers
            .Single(x => x.PricingTierId == vipTierId);

        Assert.AreEqual("VIP", vip.Name);
        Assert.AreEqual(300m, vip.Price);
        Assert.AreEqual(100, vip.Capacity);
        Assert.AreEqual(20, vip.AvailableQuantity);

        var standard = result.PricingTiers
            .Single(x => x.PricingTierId == standardTierId);

        Assert.AreEqual("Standard", standard.Name);
        Assert.AreEqual(100m, standard.Price);
        Assert.AreEqual(200, standard.Capacity);
        Assert.AreEqual(150, standard.AvailableQuantity);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_WhenAllTicketsAreSold_ReturnsZeroAvailable()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var events = new List<Event>
        {
            new Event
            {
                Id = eventId,
                Name = "Sold Out Event",
                TotalCapacity = 200,
                PricingTiers = new List<PricingTier>
                {
                    new PricingTier
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventId,
                        Name = "Standard",
                        Price = 100m,
                        Capacity = 200,
                        AvailableQuantity = 0
                    }
                }
            }
        }.BuildMockDbSet();

        var mockDb = new Mock<ITicketingDbContext>();

        mockDb.Setup(x => x.Events)
            .Returns(events.Object);

        var service = new TicketService(mockDb.Object);

        // Act
        var result = await service.GetAvailabilityAsync(
            eventId,
            CancellationToken.None);

        // Assert
        Assert.AreEqual(0, result.TotalAvailable);
        Assert.HasCount(1, result.PricingTiers);
        Assert.AreEqual(
            0,
            result.PricingTiers[0].AvailableQuantity);
    }
}