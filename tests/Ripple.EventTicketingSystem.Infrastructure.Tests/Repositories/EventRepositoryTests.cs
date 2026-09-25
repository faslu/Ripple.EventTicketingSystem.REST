using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ripple.EventTicketingSystem.Application.DTOs.Reports;
using Ripple.EventTicketingSystem.Domain.Models;
using Ripple.EventTicketingSystem.Infrastructure.Data;
using Ripple.EventTicketingSystem.Infrastructure.Repositories;

namespace Ripple.EventTicketingSystem.Infrastructure.Tests.Repositories;

#region InMemory Repository Tests

[TestClass]
public class EventRepositoryTests
{
    private TicketingDbContext _db = null!;
    private EventRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new TicketingDbContext(options);
        _repository = new EventRepository(_db);
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenEventExists_ReturnsEvent()
    {
        var eventId = Guid.NewGuid();
        var eventEntity = CreateEvent(eventId, "Music Festival");

        _db.Events.Add(eventEntity);
        await _db.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(
            eventId,
            cancellationToken: CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(eventId, result.Id);
        Assert.AreEqual("Music Festival", result.Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenEventDoesNotExist_ReturnsNull()
    {
        var eventId = Guid.NewGuid();

        var result = await _repository.GetByIdAsync(
            eventId,
            cancellationToken: CancellationToken.None);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenIncludePricingTiersIsTrue_ReturnsPricingTiers()
    {
        var eventId = Guid.NewGuid();

        var eventEntity = CreateEvent(
            eventId,
            "Music Festival");

        eventEntity.PricingTiers.Add(
            CreatePricingTier(
                eventId,
                "VIP",
                300m,
                100,
                20));

        eventEntity.PricingTiers.Add(
            CreatePricingTier(
                eventId,
                "Standard",
                100m,
                200,
                150));

        _db.Events.Add(eventEntity);
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();

        var result = await _repository.GetByIdAsync(
            eventId,
            includePricingTiers: true,
            cancellationToken: CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.PricingTiers);
        Assert.HasCount(2, result.PricingTiers);

        Assert.IsTrue(
            result.PricingTiers.Any(x => x.Name == "VIP"));

        Assert.IsTrue(
            result.PricingTiers.Any(x => x.Name == "Standard"));
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenIncludePricingTiersIsFalse_DoesNotLoadPricingTiers()
    {
        var eventId = Guid.NewGuid();

        var eventEntity = CreateEvent(
            eventId,
            "Music Festival");

        eventEntity.PricingTiers.Add(
            CreatePricingTier(
                eventId,
                "VIP",
                300m,
                100,
                20));

        _db.Events.Add(eventEntity);
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();

        var result = await _repository.GetByIdAsync(
            eventId,
            includePricingTiers: false,
            cancellationToken: CancellationToken.None);

        Assert.IsNotNull(result);

        Assert.HasCount(
            0,
            result.PricingTiers);
    }

    [TestMethod]
    public async Task GetAllAsync_WhenEventsExist_ReturnsAllEvents()
    {
        var event1 = CreateEvent(
            Guid.NewGuid(),
            "Event A");

        var event2 = CreateEvent(
            Guid.NewGuid(),
            "Event B");

        var event3 = CreateEvent(
            Guid.NewGuid(),
            "Event C");

        _db.Events.AddRange(
            event1,
            event2,
            event3);

        await _db.SaveChangesAsync();

        var result = await _repository.GetAllAsync(
            CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.HasCount(3, result);
    }

    [TestMethod]
    public async Task GetAllAsync_WhenEventsExist_ReturnsOrderedByEventDateAndTime()
    {
        var event1 = CreateEvent(
            Guid.NewGuid(),
            "Later Event",
            new DateTime(2026, 12, 20),
            new TimeSpan(10, 0, 0));

        var event2 = CreateEvent(
            Guid.NewGuid(),
            "Earlier Event",
            new DateTime(2026, 10, 10),
            new TimeSpan(12, 0, 0));

        var event3 = CreateEvent(
            Guid.NewGuid(),
            "Same Day Earlier Time",
            new DateTime(2026, 10, 10),
            new TimeSpan(9, 0, 0));

        _db.Events.AddRange(
            event1,
            event2,
            event3);

        await _db.SaveChangesAsync();

        var result = await _repository.GetAllAsync(
            CancellationToken.None);

        Assert.HasCount(3, result);

        Assert.AreEqual(
            "Same Day Earlier Time",
            result[0].Name);

        Assert.AreEqual(
            "Earlier Event",
            result[1].Name);

        Assert.AreEqual(
            "Later Event",
            result[2].Name);
    }

    [TestMethod]
    public async Task GetAllAsync_WhenEventsHavePricingTiers_ReturnsPricingTiers()
    {
        var eventId = Guid.NewGuid();

        var eventEntity = CreateEvent(
            eventId,
            "Music Festival");

        eventEntity.PricingTiers.Add(
            CreatePricingTier(
                eventId,
                "VIP",
                300m,
                100,
                20));

        eventEntity.PricingTiers.Add(
            CreatePricingTier(
                eventId,
                "Standard",
                100m,
                200,
                150));

        _db.Events.Add(eventEntity);
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();

        var result = await _repository.GetAllAsync(
            CancellationToken.None);

        Assert.HasCount(1, result);
        Assert.HasCount(2, result[0].PricingTiers);
    }

    [TestMethod]
    public async Task ExistsAsync_WhenEventExists_ReturnsTrue()
    {
        var eventId = Guid.NewGuid();

        _db.Events.Add(
            CreateEvent(
                eventId,
                "Music Festival"));

        await _db.SaveChangesAsync();

        var result = await _repository.ExistsAsync(
            eventId,
            CancellationToken.None);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ExistsAsync_WhenEventDoesNotExist_ReturnsFalse()
    {
        var eventId = Guid.NewGuid();

        var result = await _repository.ExistsAsync(
            eventId,
            CancellationToken.None);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task Add_WhenEventIsAdded_SavesEvent()
    {
        var eventId = Guid.NewGuid();

        var eventEntity = CreateEvent(
            eventId,
            "Music Festival");

        _repository.Add(eventEntity);

        await _db.SaveChangesAsync();

        var savedEvent = await _db.Events
            .SingleOrDefaultAsync(x => x.Id == eventId);

        Assert.IsNotNull(savedEvent);
        Assert.AreEqual(
            "Music Festival",
            savedEvent.Name);
    }

    [TestMethod]
    public async Task Remove_WhenEventExists_RemovesEvent()
    {
        var eventId = Guid.NewGuid();

        var eventEntity = CreateEvent(
            eventId,
            "Music Festival");

        _db.Events.Add(eventEntity);

        await _db.SaveChangesAsync();

        _repository.Remove(eventEntity);

        await _db.SaveChangesAsync();

        var exists = await _db.Events
            .AnyAsync(x => x.Id == eventId);

        Assert.IsFalse(exists);
    }

    private static Event CreateEvent(
        Guid id,
        string name,
        DateTime? eventDate = null,
        TimeSpan? eventTime = null)
    {
        return new Event
        {
            Id = id,
            Name = name,
            Description = $"{name} description",
            Venue = "Sydney Convention Centre",
            EventDate = eventDate
                ?? new DateTime(2026, 11, 15),
            EventTime = eventTime
                ?? new TimeSpan(18, 0, 0),
            TotalCapacity = 300,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PricingTier CreatePricingTier(
        Guid eventId,
        string name,
        decimal price,
        int capacity,
        int availableQuantity)
    {
        return new PricingTier
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Name = name,
            Price = price,
            Capacity = capacity,
            AvailableQuantity = availableQuantity
        };
    }
}

#endregion


#region SQL Server Sales Summary Tests

[TestClass]
public class EventRepositorySqlServerTests
{
    private TicketingDbContext _db = null!;
    private EventRepository _repository = null!;

    private string _databaseName = null!;

    /*
     * Uses SQL Server LocalDB.
     *
     * LocalDB is normally installed with Visual Studio.
     *
     * If your machine uses a different SQL Server instance,
     * change the connection string below.
     */
    private const string SqlServerConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True;";

    [TestInitialize]
    public async Task TestInitialize()
    {
        _databaseName =
            $"RippleEventTicketingTests_{Guid.NewGuid():N}";

        var connectionString =
            $"{SqlServerConnectionString};Database={_databaseName}";

        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _db = new TicketingDbContext(options);
        _repository = new EventRepository(_db);

        /*
         * Creates the test database and schema.
         *
         * This will also create the SQL Server computed column:
         *
         * TotalAmount AS ([Quantity] * [UnitPrice])
         */
        await _db.Database.EnsureCreatedAsync();
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [TestMethod]
    public async Task GetSalesSummaryAsync_WhenNoEvents_ReturnsEmptyList()
    {
        var result =
            await _repository.GetSalesSummaryAsync(
                CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.HasCount(0, result);
    }

    [TestMethod]
    public async Task GetSalesSummaryAsync_WhenEventHasNoTickets_ReturnsZeroSales()
    {
        var eventId = Guid.NewGuid();

        _db.Events.Add(
            CreateEvent(
                eventId,
                "Tech Expo"));

        await _db.SaveChangesAsync();

        var result =
            await _repository.GetSalesSummaryAsync(
                CancellationToken.None);

        Assert.HasCount(1, result);

        Assert.AreEqual(
            eventId,
            result[0].EventId);

        Assert.AreEqual(
            "Tech Expo",
            result[0].EventName);

        Assert.AreEqual(
            0,
            result[0].TicketsSold);

        Assert.AreEqual(
            0m,
            result[0].Revenue);
    }

    [TestMethod]
    public async Task GetSalesSummaryAsync_WhenEventHasTickets_CalculatesTicketsSold()
    {
        var eventId = Guid.NewGuid();

        var eventEntity =
            CreateEvent(
                eventId,
                "Music Festival");

        AddTicket(
            eventEntity,
            quantity: 2,
            unitPrice: 100m);

        AddTicket(
            eventEntity,
            quantity: 3,
            unitPrice: 150m);

        _db.Events.Add(eventEntity);

        await _db.SaveChangesAsync();

        var result =
            await _repository.GetSalesSummaryAsync(
                CancellationToken.None);

        Assert.HasCount(1, result);

        Assert.AreEqual(
            5,
            result[0].TicketsSold);
    }

    [TestMethod]
    public async Task GetSalesSummaryAsync_WhenEventHasTickets_CalculatesRevenue()
    {
        var eventId = Guid.NewGuid();

        var eventEntity =
            CreateEvent(
                eventId,
                "Music Festival");

        AddTicket(
            eventEntity,
            quantity: 2,
            unitPrice: 100m);

        AddTicket(
            eventEntity,
            quantity: 3,
            unitPrice: 150m);

        _db.Events.Add(eventEntity);

        await _db.SaveChangesAsync();

        var result =
            await _repository.GetSalesSummaryAsync(
                CancellationToken.None);

        Assert.HasCount(1, result);

        /*
         * SQL Server calculates:
         *
         * 2 * 100 = 200
         * 3 * 150 = 450
         *
         * Total = 650
         */
        Assert.AreEqual(
            650m,
            result[0].Revenue);
    }

    [TestMethod]
    public async Task GetSalesSummaryAsync_WhenMultipleEvents_ReturnsOrderedByRevenueDescending()
    {
        var eventA =
            CreateEvent(
                Guid.NewGuid(),
                "Event A");

        AddTicket(
            eventA,
            quantity: 1,
            unitPrice: 100m);

        var eventB =
            CreateEvent(
                Guid.NewGuid(),
                "Event B");

        AddTicket(
            eventB,
            quantity: 5,
            unitPrice: 100m);

        var eventC =
            CreateEvent(
                Guid.NewGuid(),
                "Event C");

        AddTicket(
            eventC,
            quantity: 2,
            unitPrice: 150m);

        _db.Events.AddRange(
            eventA,
            eventB,
            eventC);

        await _db.SaveChangesAsync();

        var result =
            await _repository.GetSalesSummaryAsync(
                CancellationToken.None);

        Assert.HasCount(3, result);

        Assert.AreEqual(
            "Event B",
            result[0].EventName);

        Assert.AreEqual(
            500m,
            result[0].Revenue);

        Assert.AreEqual(
            "Event C",
            result[1].EventName);

        Assert.AreEqual(
            300m,
            result[1].Revenue);

        Assert.AreEqual(
            "Event A",
            result[2].EventName);

        Assert.AreEqual(
            100m,
            result[2].Revenue);
    }

    [TestMethod]
    public async Task GetSalesSummaryAsync_WhenEventHasMultipleTickets_CalculatesTotalsCorrectly()
    {
        var eventId = Guid.NewGuid();

        var eventEntity =
            CreateEvent(
                eventId,
                "Concert");

        AddTicket(
            eventEntity,
            quantity: 2,
            unitPrice: 100m);

        AddTicket(
            eventEntity,
            quantity: 4,
            unitPrice: 75m);

        AddTicket(
            eventEntity,
            quantity: 1,
            unitPrice: 500m);

        _db.Events.Add(eventEntity);

        await _db.SaveChangesAsync();

        var result =
            await _repository.GetSalesSummaryAsync(
                CancellationToken.None);

        Assert.HasCount(1, result);

        /*
         * Tickets sold:
         * 2 + 4 + 1 = 7
         *
         * Revenue:
         * 2 * 100 = 200
         * 4 * 75  = 300
         * 1 * 500 = 500
         *
         * Total = 1000
         */
        Assert.AreEqual(
            7,
            result[0].TicketsSold);

        Assert.AreEqual(
            1000m,
            result[0].Revenue);
    }

    [TestMethod]
    public async Task GetSalesSummaryAsync_WhenEventsHaveAndDoNotHaveTickets_ReturnsAllEvents()
    {
        var eventWithTickets =
            CreateEvent(
                Guid.NewGuid(),
                "Event With Tickets");

        AddTicket(
            eventWithTickets,
            quantity: 2,
            unitPrice: 100m);

        var eventWithoutTickets =
            CreateEvent(
                Guid.NewGuid(),
                "Event Without Tickets");

        _db.Events.AddRange(
            eventWithTickets,
            eventWithoutTickets);

        await _db.SaveChangesAsync();

        var result =
            await _repository.GetSalesSummaryAsync(
                CancellationToken.None);

        Assert.HasCount(2, result);

        var eventWithSales =
            result.Single(
                x => x.EventName == "Event With Tickets");

        Assert.AreEqual(
            2,
            eventWithSales.TicketsSold);

        Assert.AreEqual(
            200m,
            eventWithSales.Revenue);

        var eventWithoutSales =
            result.Single(
                x => x.EventName == "Event Without Tickets");

        Assert.AreEqual(
            0,
            eventWithoutSales.TicketsSold);

        Assert.AreEqual(
            0m,
            eventWithoutSales.Revenue);
    }

    private static Event CreateEvent(
        Guid id,
        string name,
        DateTime? eventDate = null,
        TimeSpan? eventTime = null)
    {
        return new Event
        {
            Id = id,
            Name = name,
            Description = $"{name} description",
            Venue = "Sydney Convention Centre",
            EventDate = eventDate
                ?? new DateTime(2026, 11, 15),
            EventTime = eventTime
                ?? new TimeSpan(18, 0, 0),
            TotalCapacity = 300,
            CreatedAt = DateTime.UtcNow
        };
    }

    /*
     * Creates a PricingTier and Ticket together.
     *
     * This is necessary for SQL Server because Ticket.PricingTierId
     * has a foreign-key relationship to PricingTiers.
     */
    private static void AddTicket(
        Event eventEntity,
        int quantity,
        decimal unitPrice)
    {
        var pricingTier =
            new PricingTier
            {
                Id = Guid.NewGuid(),
                EventId = eventEntity.Id,
                Name = $"Tier {eventEntity.PricingTiers.Count + 1}",
                Price = unitPrice,
                Capacity = 300,
                AvailableQuantity = 300
            };

        var ticket =
            new Ticket(
                quantity,
                unitPrice)
            {
                Id = Guid.NewGuid(),
                EventId = eventEntity.Id,
                PricingTierId = pricingTier.Id,
                CustomerName = "Test Customer",
                CustomerEmail = "test@example.com",
                PurchaseDate = DateTime.UtcNow
            };

        eventEntity.PricingTiers.Add(pricingTier);
        eventEntity.Tickets.Add(ticket);
    }
}

#endregion
