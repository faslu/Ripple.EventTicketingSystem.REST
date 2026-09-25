using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Domain.Exceptions;
using Ripple.EventTicketingSystem.Infrastructure.Data;
using Ripple.EventTicketingSystem.Infrastructure.Repositories;

namespace Ripple.EventTicketingSystem.Infrastructure.Tests.Repositories;

[TestClass]
public class UnitOfWorkTests
{
    private SqliteConnection _connection = null!;
    private TicketingDbContext _dbContext = null!;

    private Mock<IEventRepository> _eventRepository = null!;
    private Mock<IPricingTierRepository> _pricingTierRepository = null!;
    private Mock<ITicketRepository> _ticketRepository = null!;

    private UnitOfWork _unitOfWork = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TicketingDbContext(options);

        _dbContext.Database.EnsureCreated();

        _eventRepository = new Mock<IEventRepository>();
        _pricingTierRepository = new Mock<IPricingTierRepository>();
        _ticketRepository = new Mock<ITicketRepository>();

        _unitOfWork = new UnitOfWork(
            _dbContext,
            _eventRepository.Object,
            _pricingTierRepository.Object,
            _ticketRepository.Object);
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [TestMethod]
    public async Task SaveChangesAsync_WhenEntitiesAreAdded_SavesChanges()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var eventEntity = new Domain.Models.Event
        {
            Id = eventId,
            Name = "Test Event",
            Description = "Test Description",
            Venue = "Test Venue",
            EventDate = DateTime.UtcNow.Date.AddDays(30),
            EventTime = new TimeSpan(19, 0, 0),
            TotalCapacity = 100,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Events.Add(eventEntity);

        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        Assert.AreEqual(1, result);

        var savedEvent = await _dbContext.Events
            .SingleAsync(x => x.Id == eventId);

        Assert.AreEqual("Test Event", savedEvent.Name);
    }

    [TestMethod]
    public async Task ExecuteInTransactionAsync_WhenOperationSucceeds_CommitsTransaction()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        await _unitOfWork.ExecuteInTransactionAsync(
            async cancellationToken =>
            {
                _dbContext.Events.Add(
                    new Domain.Models.Event
                    {
                        Id = eventId,
                        Name = "Transaction Event",
                        Description = "Test",
                        Venue = "Test Venue",
                        EventDate = DateTime.UtcNow.Date.AddDays(30),
                        EventTime = new TimeSpan(19, 0, 0),
                        TotalCapacity = 100,
                        CreatedAt = DateTime.UtcNow
                    });

                await _dbContext.SaveChangesAsync(cancellationToken);
            });

        // Assert
        var savedEvent = await _dbContext.Events
            .SingleOrDefaultAsync(x => x.Id == eventId);

        Assert.IsNotNull(savedEvent);
        Assert.AreEqual("Transaction Event", savedEvent.Name);
    }

    [TestMethod]
    public async Task ExecuteInTransactionAsync_WhenOperationThrows_RollsBackTransaction()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () =>
            {
                await _unitOfWork.ExecuteInTransactionAsync(
                    async cancellationToken =>
                    {
                        _dbContext.Events.Add(
                            new Domain.Models.Event
                            {
                                Id = eventId,
                                Name = "Should Rollback",
                                Description = "Test",
                                Venue = "Test Venue",
                                EventDate = DateTime.UtcNow.Date.AddDays(30),
                                EventTime = new TimeSpan(19, 0, 0),
                                TotalCapacity = 100,
                                CreatedAt = DateTime.UtcNow
                            });

                        await _dbContext.SaveChangesAsync(
                            cancellationToken);

                        throw new InvalidOperationException(
                            "Simulated failure.");
                    });
            });

        // Assert
        var savedEvent = await _dbContext.Events
            .SingleOrDefaultAsync(x => x.Id == eventId);

        Assert.IsNull(savedEvent);
    }

    [TestMethod]
    public async Task ExecuteInTransactionAsync_WhenOperationThrowsConflictException_RollsBackAndRethrows()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act & Assert
        var exception =
            await Assert.ThrowsExactlyAsync<ConflictException>(
                async () =>
                {
                    await _unitOfWork.ExecuteInTransactionAsync(
                        async cancellationToken =>
                        {
                            _dbContext.Events.Add(
                                new Domain.Models.Event
                                {
                                    Id = eventId,
                                    Name = "Conflict Event",
                                    Description = "Test",
                                    Venue = "Test Venue",
                                    EventDate = DateTime.UtcNow.Date.AddDays(30),
                                    EventTime = new TimeSpan(19, 0, 0),
                                    TotalCapacity = 100,
                                    CreatedAt = DateTime.UtcNow
                                });

                            await _dbContext.SaveChangesAsync(
                                cancellationToken);

                            throw new ConflictException(
                                "Simulated conflict.");
                        });
                });

        Assert.AreEqual(
            "Simulated conflict.",
            exception.Message);

        var savedEvent = await _dbContext.Events
            .SingleOrDefaultAsync(x => x.Id == eventId);

        Assert.IsNull(savedEvent);
    }

    [TestMethod]
    public async Task ExecuteInTransactionAsync_PassesCancellationTokenToOperation()
    {
        // Arrange
        using var cancellationTokenSource =
            new CancellationTokenSource();

        var expectedToken = cancellationTokenSource.Token;

        CancellationToken actualToken = default;

        // Act
        await _unitOfWork.ExecuteInTransactionAsync(
            cancellationToken =>
            {
                actualToken = cancellationToken;
                return Task.CompletedTask;
            },
            expectedToken);

        // Assert
        Assert.AreEqual(expectedToken, actualToken);
    }
}
