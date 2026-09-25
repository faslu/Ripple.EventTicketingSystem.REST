using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ripple.EventTicketingSystem.Domain.Models;
using Ripple.EventTicketingSystem.Infrastructure.Data;
using Ripple.EventTicketingSystem.Infrastructure.Repositories;
using System.Timers;

namespace Ripple.EventTicketingSystem.Infrastructure.Tests.Repositories;

[TestClass]
public class PricingTierRepositoryTests
{
    private TicketingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TicketingDbContext(options);
    }

    [TestMethod]
    public async Task GetForEventAsync_WhenPricingTierExistsForEvent_ReturnsPricingTier()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var pricingTierId = Guid.NewGuid();

        await using var db = CreateDbContext();

        var pricingTier = new PricingTier
        {
            Id = pricingTierId,
            EventId = eventId,
            Name = "VIP",
            Price = 150.00m,
            Capacity = 100
        };

        db.PricingTiers.Add(pricingTier);
        await db.SaveChangesAsync();

        var repository = new PricingTierRepository(db);

        // Act
        var result = await repository.GetForEventAsync(
            pricingTierId,
            eventId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(pricingTierId, result.Id);
        Assert.AreEqual(eventId, result.EventId);
        Assert.AreEqual("VIP", result.Name);
        Assert.AreEqual(150.00m, result.Price);
        Assert.AreEqual(100, result.Capacity);
    }

    [TestMethod]
    public async Task GetForEventAsync_WhenPricingTierDoesNotExist_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var pricingTierId = Guid.NewGuid();

        await using var db = CreateDbContext();

        var repository = new PricingTierRepository(db);

        // Act
        var result = await repository.GetForEventAsync(
            pricingTierId,
            eventId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetForEventAsync_WhenPricingTierBelongsToDifferentEvent_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var differentEventId = Guid.NewGuid();
        var pricingTierId = Guid.NewGuid();

        await using var db = CreateDbContext();

        var pricingTier = new PricingTier
        {
            Id = pricingTierId,
            EventId = differentEventId,
            Name = "VIP",
            Price = 150.00m,
            Capacity = 100
        };

        db.PricingTiers.Add(pricingTier);
        await db.SaveChangesAsync();

        var repository = new PricingTierRepository(db);

        // Act
        var result = await repository.GetForEventAsync(
            pricingTierId,
            eventId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetForEventAsync_WhenEventHasDifferentPricingTier_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var pricingTierId = Guid.NewGuid();
        var differentPricingTierId = Guid.NewGuid();

        await using var db = CreateDbContext();

        var pricingTier = new PricingTier
        {
            Id = differentPricingTierId,
            EventId = eventId,
            Name = "Standard",
            Price = 50.00m,
            Capacity = 200
        };

        db.PricingTiers.Add(pricingTier);
        await db.SaveChangesAsync();

        var repository = new PricingTierRepository(db);

        // Act
        var result = await repository.GetForEventAsync(
            pricingTierId,
            eventId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetForEventAsync_WhenMultiplePricingTiersExist_ReturnsCorrectPricingTier()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var standardTierId = Guid.NewGuid();
        var vipTierId = Guid.NewGuid();
        var premiumTierId = Guid.NewGuid();

        await using var db = CreateDbContext();

        db.PricingTiers.AddRange(
            new PricingTier
            {
                Id = standardTierId,
                EventId = eventId,
                Name = "Standard",
                Price = 50.00m,
                Capacity = 500
            },
            new PricingTier
            {
                Id = vipTierId,
                EventId = eventId,
                Name = "VIP",
                Price = 150.00m,
                Capacity = 100
            },
            new PricingTier
            {
                Id = premiumTierId,
                EventId = eventId,
                Name = "Premium",
                Price = 250.00m,
                Capacity = 50
            });

        await db.SaveChangesAsync();

        var repository = new PricingTierRepository(db);

        // Act
        var result = await repository.GetForEventAsync(
            vipTierId,
            eventId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(vipTierId, result.Id);
        Assert.AreEqual(eventId, result.EventId);
        Assert.AreEqual("VIP", result.Name);
        Assert.AreEqual(150.00m, result.Price);
        Assert.AreEqual(100, result.Capacity);
    }

    [TestMethod]
    public async Task GetForEventAsync_WhenCancellationTokenIsNotCancelled_ReturnsPricingTier()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var pricingTierId = Guid.NewGuid();

        await using var db = CreateDbContext();

        db.PricingTiers.Add(new PricingTier
        {
            Id = pricingTierId,
            EventId = eventId,
            Name = "Standard",
            Price = 75.00m,
            Capacity = 250
        });

        await db.SaveChangesAsync();

        var repository = new PricingTierRepository(db);
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var result = await repository.GetForEventAsync(
            pricingTierId,
            eventId,
            cancellationTokenSource.Token);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(pricingTierId, result.Id);
    }
}