using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ripple.EventTicketingSystem.Application.DTOs.Tickets;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Application.Options;
using Ripple.EventTicketingSystem.Application.Services;
using Ripple.EventTicketingSystem.Domain.Exceptions;
using Ripple.EventTicketingSystem.Domain.Models;
using static System.Net.Mime.MediaTypeNames;

namespace Ripple.EventTicketingSystem.Application.Tests.Services;

[TestClass]
public class TicketServiceTests
{
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IEventRepository> _eventRepository = null!;
    private Mock<IPricingTierRepository> _pricingTierRepository = null!;
    private Mock<ITicketRepository> _ticketRepository = null!;
    private TicketService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _eventRepository = new Mock<IEventRepository>();
        _pricingTierRepository = new Mock<IPricingTierRepository>();
        _ticketRepository = new Mock<ITicketRepository>();

        _unitOfWork
            .SetupGet(x => x.Events)
            .Returns(_eventRepository.Object);

        _unitOfWork
            .SetupGet(x => x.PricingTiers)
            .Returns(_pricingTierRepository.Object);

        _unitOfWork
            .SetupGet(x => x.Tickets)
            .Returns(_ticketRepository.Object);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Execute the supplied operation immediately.
        // Transaction behavior itself is tested in UnitOfWorkTests.
        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(
                async (operation, cancellationToken) =>
                {
                    await operation(cancellationToken);
                });

        var options = Microsoft.Extensions.Options.Options.Create(
            new TicketingOptions
            {
                MaxTicketsPerPurchase = 10
            });

        _service = new TicketService(
            _unitOfWork.Object,
            options);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenEventDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _eventRepository
            .Setup(x => x.ExistsAsync(
                eventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = CreatePurchaseRequest();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<NotFoundException>(
            () => _service.PurchaseAsync(
                eventId,
                request,
                CancellationToken.None));

        _eventRepository.Verify(
            x => x.ExistsAsync(
                eventId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _pricingTierRepository.Verify(
            x => x.GetForEventAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenQuantityExceedsMaximum_ThrowsValidationException()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var request = CreatePurchaseRequest(
            quantity: 11);

        // Act & Assert
        var exception =
            await Assert.ThrowsExactlyAsync<ValidationException>(
                () => _service.PurchaseAsync(
                    eventId,
                    request,
                    CancellationToken.None));

        Assert.AreEqual(
            "You can purchase a maximum of 10 tickets per purchase.",
            exception.Message);

        _eventRepository.Verify(
            x => x.ExistsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenPricingTierDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        _eventRepository
            .Setup(x => x.ExistsAsync(
                eventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _pricingTierRepository
            .Setup(x => x.GetForEventAsync(
                tierId,
                eventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingTier?)null);

        var request = CreatePurchaseRequest(
            pricingTierId: tierId);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<NotFoundException>(
            () => _service.PurchaseAsync(
                eventId,
                request,
                CancellationToken.None));

        _pricingTierRepository.Verify(
            x => x.GetForEventAsync(
                tierId,
                eventId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenPricingTierBelongsToAnotherEvent_ThrowsNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var anotherEventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        _eventRepository
            .Setup(x => x.ExistsAsync(
                eventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // The repository is expected to enforce the event/tier relationship
        // and return null when the tier does not belong to the event.
        _pricingTierRepository
            .Setup(x => x.GetForEventAsync(
                tierId,
                eventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingTier?)null);

        var request = CreatePurchaseRequest(
            pricingTierId: tierId);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<NotFoundException>(
            () => _service.PurchaseAsync(
                eventId,
                request,
                CancellationToken.None));

        _pricingTierRepository.Verify(
            x => x.GetForEventAsync(
                tierId,
                eventId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenInsufficientQuantity_ThrowsConflictException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var tier = CreatePricingTier(
            eventId,
            tierId,
            availableQuantity: 2,
            price: 200m);

        _eventRepository
            .Setup(x => x.ExistsAsync(
                eventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _pricingTierRepository
            .Setup(x => x.GetForEventAsync(
                tierId,
                eventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tier);

        var request = CreatePurchaseRequest(
            pricingTierId: tierId,
            quantity: 5);

        // Act & Assert
        var exception =
            await Assert.ThrowsExactlyAsync<ConflictException>(
                () => _service.PurchaseAsync(
                    eventId,
                    request,
                    CancellationToken.None));

        Assert.AreEqual(
            "Only 2 tickets are available.",
            exception.Message);

        Assert.AreEqual(
            2,
            tier.AvailableQuantity);

        _ticketRepository.Verify(
            x => x.Add(It.IsAny<Ticket>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenExactAvailableQuantityIsRequested_Succeeds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var tier = CreatePricingTier(
            eventId,
            tierId,
            availableQuantity: 2,
            price: 100m);

        SetupSuccessfulPurchase(
            eventId,
            tierId,
            tier);

        var request = CreatePurchaseRequest(
            pricingTierId: tierId,
            quantity: 2);

        // Act
        var result = await _service.PurchaseAsync(
            eventId,
            request,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Quantity);
        Assert.AreEqual(0, tier.AvailableQuantity);

        _ticketRepository.Verify(
            x => x.Add(It.IsAny<Ticket>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenValid_ReturnsCorrectTicketResponse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var tier = CreatePricingTier(
            eventId,
            tierId,
            availableQuantity: 10,
            price: 100m,
            name: "Standard");

        SetupSuccessfulPurchase(
            eventId,
            tierId,
            tier);

        var request = CreatePurchaseRequest(
            pricingTierId: tierId,
            quantity: 2,
            customerName: "John",
            customerEmail: "john@example.com");

        // Act
        var result = await _service.PurchaseAsync(
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

        var tier = CreatePricingTier(
            eventId,
            tierId,
            availableQuantity: 10,
            price: 75m);

        SetupSuccessfulPurchase(
            eventId,
            tierId,
            tier);

        var request = CreatePurchaseRequest(
            pricingTierId: tierId,
            quantity: 1,
            customerName: "  John Smith  ",
            customerEmail: "  john@example.com  ");

        // Act
        var result = await _service.PurchaseAsync(
            eventId,
            request,
            CancellationToken.None);

        // Assert
        Assert.AreEqual(
            "John Smith",
            result.CustomerName);

        Assert.AreEqual(
            "john@example.com",
            result.CustomerEmail);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenValid_AddsTicketWithCorrectValues()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var tier = CreatePricingTier(
            eventId,
            tierId,
            availableQuantity: 20,
            price: 250m,
            name: "VIP");

        SetupSuccessfulPurchase(
            eventId,
            tierId,
            tier);

        Ticket? addedTicket = null;

        _ticketRepository
            .Setup(x => x.Add(It.IsAny<Ticket>()))
            .Callback<Ticket>(ticket =>
            {
                addedTicket = ticket;
            });

        var request = CreatePurchaseRequest(
            pricingTierId: tierId,
            quantity: 3,
            customerName: "Jane Smith",
            customerEmail: "jane@example.com");

        // Act
        await _service.PurchaseAsync(
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

        Assert.AreNotEqual(Guid.Empty, addedTicket.Id);
        Assert.AreNotEqual(default, addedTicket.PurchaseDate);

        _ticketRepository.Verify(
            x => x.Add(It.IsAny<Ticket>()),
            Times.Once);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenValid_ReducesAvailableQuantity()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var tier = CreatePricingTier(
            eventId,
            tierId,
            availableQuantity: 10,
            price: 100m);

        SetupSuccessfulPurchase(
            eventId,
            tierId,
            tier);

        var request = CreatePurchaseRequest(
            pricingTierId: tierId,
            quantity: 4);

        // Act
        await _service.PurchaseAsync(
            eventId,
            request,
            CancellationToken.None);

        // Assert
        Assert.AreEqual(
            6,
            tier.AvailableQuantity);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenValid_CallsSaveChangesOnce()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var tier = CreatePricingTier(
            eventId,
            tierId,
            availableQuantity: 10,
            price: 100m);

        SetupSuccessfulPurchase(
            eventId,
            tierId,
            tier);

        var request = CreatePurchaseRequest(
            pricingTierId: tierId,
            quantity: 1);

        // Act
        await _service.PurchaseAsync(
            eventId,
            request,
            CancellationToken.None);

        // Assert
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenValid_ExecutesOperationInTransaction()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        var tier = CreatePricingTier(
            eventId,
            tierId,
            availableQuantity: 10,
            price: 100m);

        SetupSuccessfulPurchase(
            eventId,
            tierId,
            tier);

        var request = CreatePurchaseRequest(
            pricingTierId: tierId,
            quantity: 2);

        // Act
        await _service.PurchaseAsync(
            eventId,
            request,
            CancellationToken.None);

        // Assert
        _unitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task PurchaseAsync_WhenCancellationTokenProvided_PassesItToRepository()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var tier = CreatePricingTier(
            eventId,
            tierId,
            availableQuantity: 10,
            price: 100m);

        _eventRepository
            .Setup(x => x.ExistsAsync(
                eventId,
                cancellationToken))
            .ReturnsAsync(true);

        _pricingTierRepository
            .Setup(x => x.GetForEventAsync(
                tierId,
                eventId,
                cancellationToken))
            .ReturnsAsync(tier);

        var request = CreatePurchaseRequest(
            pricingTierId: tierId,
            quantity: 1);

        // Act
        await _service.PurchaseAsync(
            eventId,
            request,
            cancellationToken);

        // Assert
        _eventRepository.Verify(
            x => x.ExistsAsync(
                eventId,
                cancellationToken),
            Times.Once);

        _pricingTierRepository.Verify(
            x => x.GetForEventAsync(
                tierId,
                eventId,
                cancellationToken),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                cancellationToken),
            Times.Once);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_WhenEventDoesNotExist_ThrowsNotFoundException()
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
            () => _service.GetAvailabilityAsync(
                eventId,
                CancellationToken.None));

        _eventRepository.Verify(
            x => x.GetByIdAsync(
                eventId,
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_WhenValid_ReturnsEventDetails()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var eventEntity = new Event
        {
            Id = eventId,
            Name = "Tech Expo",
            TotalCapacity = 300,
            PricingTiers = new List<PricingTier>
            {
                CreatePricingTier(
                    eventId,
                    Guid.NewGuid(),
                    availableQuantity: 20,
                    price: 300m,
                    name: "VIP",
                    capacity: 100),

                CreatePricingTier(
                    eventId,
                    Guid.NewGuid(),
                    availableQuantity: 150,
                    price: 100m,
                    name: "Standard",
                    capacity: 200)
            }
        };

        SetupEventForAvailability(
            eventId,
            eventEntity);

        // Act
        var result = await _service.GetAvailabilityAsync(
            eventId,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);

        Assert.AreEqual(
            eventId,
            result.EventId);

        Assert.AreEqual(
            "Tech Expo",
            result.EventName);

        Assert.AreEqual(
            300,
            result.TotalCapacity);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_WhenValid_ComputesTotalAvailableQuantity()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var eventEntity = new Event
        {
            Id = eventId,
            Name = "Tech Expo",
            TotalCapacity = 300,
            PricingTiers = new List<PricingTier>
            {
                CreatePricingTier(
                    eventId,
                    Guid.NewGuid(),
                    availableQuantity: 20,
                    price: 300m,
                    name: "VIP",
                    capacity: 100),

                CreatePricingTier(
                    eventId,
                    Guid.NewGuid(),
                    availableQuantity: 150,
                    price: 100m,
                    name: "Standard",
                    capacity: 200)
            }
        };

        SetupEventForAvailability(
            eventId,
            eventEntity);

        // Act
        var result = await _service.GetAvailabilityAsync(
            eventId,
            CancellationToken.None);

        // Assert
        Assert.AreEqual(
            170,
            result.TotalAvailable);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_WhenValid_ReturnsAllPricingTiers()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var vipTierId = Guid.NewGuid();
        var standardTierId = Guid.NewGuid();

        var eventEntity = new Event
        {
            Id = eventId,
            Name = "Tech Expo",
            TotalCapacity = 300,
            PricingTiers = new List<PricingTier>
            {
                CreatePricingTier(
                    eventId,
                    vipTierId,
                    availableQuantity: 20,
                    price: 300m,
                    name: "VIP",
                    capacity: 100),

                CreatePricingTier(
                    eventId,
                    standardTierId,
                    availableQuantity: 150,
                    price: 100m,
                    name: "Standard",
                    capacity: 200)
            }
        };

        SetupEventForAvailability(
            eventId,
            eventEntity);

        // Act
        var result = await _service.GetAvailabilityAsync(
            eventId,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result.PricingTiers);
        Assert.HasCount(2, result.PricingTiers);

        var vip = result.PricingTiers
            .Single(x => x.PricingTierId == vipTierId);

        Assert.AreEqual(
            "VIP",
            vip.Name);

        Assert.AreEqual(
            300m,
            vip.Price);

        Assert.AreEqual(
            100,
            vip.Capacity);

        Assert.AreEqual(
            20,
            vip.AvailableQuantity);

        var standard = result.PricingTiers
            .Single(x => x.PricingTierId == standardTierId);

        Assert.AreEqual(
            "Standard",
            standard.Name);

        Assert.AreEqual(
            100m,
            standard.Price);

        Assert.AreEqual(
            200,
            standard.Capacity);

        Assert.AreEqual(
            150,
            standard.AvailableQuantity);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_WhenAllTicketsAreSold_ReturnsZeroAvailable()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var eventEntity = new Event
        {
            Id = eventId,
            Name = "Sold Out Event",
            TotalCapacity = 200,
            PricingTiers = new List<PricingTier>
            {
                CreatePricingTier(
                    eventId,
                    Guid.NewGuid(),
                    availableQuantity: 0,
                    price: 100m,
                    name: "Standard",
                    capacity: 200)
            }
        };

        SetupEventForAvailability(
            eventId,
            eventEntity);

        // Act
        var result = await _service.GetAvailabilityAsync(
            eventId,
            CancellationToken.None);

        // Assert
        Assert.AreEqual(
            0,
            result.TotalAvailable);

        Assert.HasCount(
            1,
            result.PricingTiers);

        Assert.AreEqual(
            0,
            result.PricingTiers[0].AvailableQuantity);
    }

    private void SetupSuccessfulPurchase(
        Guid eventId,
        Guid tierId,
        PricingTier tier)
    {
        _eventRepository
            .Setup(x => x.ExistsAsync(
                eventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _pricingTierRepository
            .Setup(x => x.GetForEventAsync(
                tierId,
                eventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tier);
    }

    private void SetupEventForAvailability(
        Guid eventId,
        Event eventEntity)
    {
        _eventRepository
            .Setup(x => x.GetByIdAsync(
                eventId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventEntity);
    }

    private static PurchaseTicketRequest CreatePurchaseRequest(
        Guid? pricingTierId = null,
        int quantity = 1,
        string customerName = "John",
        string customerEmail = "john@example.com")
    {
        return new PurchaseTicketRequest
        {
            PricingTierId = pricingTierId ?? Guid.NewGuid(),
            Quantity = quantity,
            CustomerName = customerName,
            CustomerEmail = customerEmail
        };
    }

    private static PricingTier CreatePricingTier(
        Guid eventId,
        Guid tierId,
        int availableQuantity,
        decimal price,
        string name = "Standard",
        int capacity = 100)
    {
        return new PricingTier
        {
            Id = tierId,
            EventId = eventId,
            Name = name,
            Price = price,
            Capacity = capacity,
            AvailableQuantity = availableQuantity
        };
    }
}