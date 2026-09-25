using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ripple.EventTicketingSystem.Domain.Models;
using Ripple.EventTicketingSystem.Infrastructure.Data;
using Ripple.EventTicketingSystem.Infrastructure.Repositories;
using System.Net.Sockets;

namespace Ripple.EventTicketingSystem.Infrastructure.Tests.Repositories;

[TestClass]
public class TicketRepositoryTests
{
    private TicketingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TicketingDbContext(options);
    }

    private static Ticket CreateTicket(Guid eventId)
    {
        var ticket = new Ticket(2, 50.00m)
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            PricingTierId = Guid.NewGuid(),
            CustomerName = "John Smith",
            CustomerEmail = "john.smith@example.com",
            PurchaseDate = DateTime.UtcNow
        };

        return ticket;
    }

    [TestMethod]
    public async Task ExistsForEventAsync_WhenTicketExistsForEvent_ReturnsTrue()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        await using var db = CreateDbContext();

        var ticket = CreateTicket(eventId);

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var repository = new TicketRepository(db);

        // Act
        var result = await repository.ExistsForEventAsync(eventId);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ExistsForEventAsync_WhenNoTicketExistsForEvent_ReturnsFalse()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        await using var db = CreateDbContext();

        var repository = new TicketRepository(db);

        // Act
        var result = await repository.ExistsForEventAsync(eventId);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ExistsForEventAsync_WhenTicketsExistForDifferentEvent_ReturnsFalse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var differentEventId = Guid.NewGuid();

        await using var db = CreateDbContext();

        var ticket = CreateTicket(differentEventId);

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var repository = new TicketRepository(db);

        // Act
        var result = await repository.ExistsForEventAsync(eventId);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ExistsForEventAsync_WhenMultipleTicketsExistForEvent_ReturnsTrue()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        await using var db = CreateDbContext();

        var ticket1 = CreateTicket(eventId);
        var ticket2 = CreateTicket(eventId);
        var ticket3 = CreateTicket(eventId);

        db.Tickets.AddRange(ticket1, ticket2, ticket3);
        await db.SaveChangesAsync();

        var repository = new TicketRepository(db);

        // Act
        var result = await repository.ExistsForEventAsync(eventId);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Add_WhenTicketIsAdded_AddsTicketToDbSet()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        await using var db = CreateDbContext();

        var repository = new TicketRepository(db);

        var ticket = CreateTicket(eventId);

        // Act
        repository.Add(ticket);

        // Assert
        Assert.AreEqual(
            EntityState.Added,
            db.Entry(ticket).State);

        Assert.IsTrue(
            db.Tickets.Local.Any(x => x.Id == ticket.Id));
    }

    [TestMethod]
    public async Task Add_WhenTicketIsAdded_DoesNotPersistUntilSaveChanges()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        await using var db = CreateDbContext();

        var repository = new TicketRepository(db);

        var ticket = CreateTicket(eventId);

        // Act
        repository.Add(ticket);

        // Assert
        Assert.AreEqual(
            EntityState.Added,
            db.Entry(ticket).State);

        // Add() only tracks the entity.
        // It does not persist it to the database.
        Assert.AreEqual(
            0,
            await db.Tickets.CountAsync());

        // Explicitly save changes.
        await db.SaveChangesAsync();

        Assert.AreEqual(
            1,
            await db.Tickets.CountAsync());

        var savedTicket = await db.Tickets
            .FirstOrDefaultAsync(x => x.Id == ticket.Id);

        Assert.IsNotNull(savedTicket);
        Assert.AreEqual(eventId, savedTicket.EventId);
        Assert.AreEqual(ticket.Quantity, savedTicket.Quantity);
        Assert.AreEqual(ticket.UnitPrice, savedTicket.UnitPrice);
        Assert.AreEqual(ticket.TotalAmount, savedTicket.TotalAmount);
    }

    [TestMethod]
    public async Task ExistsForEventAsync_WhenCancellationTokenIsNotCancelled_ReturnsCorrectResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        await using var db = CreateDbContext();

        var ticket = CreateTicket(eventId);

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var repository = new TicketRepository(db);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var result = await repository.ExistsForEventAsync(
            eventId,
            cancellationTokenSource.Token);

        // Assert
        Assert.IsTrue(result);
    }
}