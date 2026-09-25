using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ripple.EventTicketingSystem.Domain.Models;
using Ripple.EventTicketingSystem.Infrastructure.Data;

namespace Ripple.EventTicketingSystem.Infrastructure.Tests.Data;

[TestClass]
public class TicketingDbContextTests
{
    private TicketingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TicketingDbContext(options);
    }

    [TestMethod]
    public void ModelContainsExpectedEntities()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var model = context.Model;

        // Assert
        Assert.IsNotNull(model.FindEntityType(typeof(Event)));
        Assert.IsNotNull(model.FindEntityType(typeof(PricingTier)));
        Assert.IsNotNull(model.FindEntityType(typeof(Ticket)));
    }

    [TestMethod]
    public void Event_HasExpectedTableName()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType = context.Model.FindEntityType(typeof(Event));

        // Assert
        Assert.IsNotNull(entityType);
        Assert.AreEqual("Events", entityType.GetTableName());
    }

    [TestMethod]
    public void PricingTier_HasExpectedTableName()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType = context.Model.FindEntityType(typeof(PricingTier));

        // Assert
        Assert.IsNotNull(entityType);
        Assert.AreEqual("PricingTiers", entityType.GetTableName());
    }

    [TestMethod]
    public void Ticket_HasExpectedTableName()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType = context.Model.FindEntityType(typeof(Ticket));

        // Assert
        Assert.IsNotNull(entityType);
        Assert.AreEqual("Tickets", entityType.GetTableName());
    }

    [TestMethod]
    public void Event_Id_IsConfiguredAsPrimaryKey()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType = context.Model.FindEntityType(typeof(Event));

        // Assert
        Assert.IsNotNull(entityType);
        Assert.AreEqual(
            nameof(Event.Id),
            entityType.FindPrimaryKey()!.Properties.Single().Name);
    }

    [TestMethod]
    public void PricingTier_Id_IsConfiguredAsPrimaryKey()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType = context.Model.FindEntityType(typeof(PricingTier));

        // Assert
        Assert.IsNotNull(entityType);
        Assert.AreEqual(
            nameof(PricingTier.Id),
            entityType.FindPrimaryKey()!.Properties.Single().Name);
    }

    [TestMethod]
    public void Ticket_Id_IsConfiguredAsPrimaryKey()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType = context.Model.FindEntityType(typeof(Ticket));

        // Assert
        Assert.IsNotNull(entityType);
        Assert.AreEqual(
            nameof(Ticket.Id),
            entityType.FindPrimaryKey()!.Properties.Single().Name);
    }

    [TestMethod]
    public void Event_Name_IsRequiredAndHasMaxLength200()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var property = context.Model
            .FindEntityType(typeof(Event))!
            .FindProperty(nameof(Event.Name));

        // Assert
        Assert.IsNotNull(property);
        Assert.IsFalse(property.IsNullable);
        Assert.AreEqual(200, property.GetMaxLength());
    }

    [TestMethod]
    public void Event_Description_IsOptionalAndHasMaxLength2000()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var property = context.Model
            .FindEntityType(typeof(Event))!
            .FindProperty(nameof(Event.Description));

        // Assert
        Assert.IsNotNull(property);
        Assert.IsTrue(property.IsNullable);
        Assert.AreEqual(2000, property.GetMaxLength());
    }

    [TestMethod]
    public void Event_Venue_IsRequiredAndHasMaxLength300()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var property = context.Model
            .FindEntityType(typeof(Event))!
            .FindProperty(nameof(Event.Venue));

        // Assert
        Assert.IsNotNull(property);
        Assert.IsFalse(property.IsNullable);
        Assert.AreEqual(300, property.GetMaxLength());
    }

    [TestMethod]
    public void PricingTier_Name_IsRequiredAndHasMaxLength100()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var property = context.Model
            .FindEntityType(typeof(PricingTier))!
            .FindProperty(nameof(PricingTier.Name));

        // Assert
        Assert.IsNotNull(property);
        Assert.IsFalse(property.IsNullable);
        Assert.AreEqual(100, property.GetMaxLength());
    }

    [TestMethod]
    public void PricingTier_RowVersion_IsConfiguredAsConcurrencyToken()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var property = context.Model
            .FindEntityType(typeof(PricingTier))!
            .FindProperty(nameof(PricingTier.RowVersion));

        // Assert
        Assert.IsNotNull(property);
        Assert.IsTrue(property.IsConcurrencyToken);
        Assert.AreNotEqual(ValueGenerated.Never, property.ValueGenerated);
    }

    [TestMethod]
    public void Event_HasPricingTiersRelationship()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType = context.Model.FindEntityType(typeof(Event));

        var navigation = entityType!
            .FindNavigation(nameof(Event.PricingTiers));

        // Assert
        Assert.IsNotNull(navigation);
        Assert.AreEqual(
            typeof(PricingTier),
            navigation.TargetEntityType.ClrType);
    }

    [TestMethod]
    public void PricingTier_HasEventRelationship()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType = context.Model.FindEntityType(typeof(PricingTier));

        var navigation = entityType!
            .FindNavigation(nameof(PricingTier.Event));

        // Assert
        Assert.IsNotNull(navigation);
        Assert.AreEqual(
            typeof(Event),
            navigation.TargetEntityType.ClrType);
    }

    [TestMethod]
    public void Ticket_HasPricingTierRelationship()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType = context.Model.FindEntityType(typeof(Ticket));

        var navigation = entityType!
            .FindNavigation(nameof(Ticket.PricingTier));

        // Assert
        Assert.IsNotNull(navigation);
        Assert.AreEqual(
            typeof(PricingTier),
            navigation.TargetEntityType.ClrType);
    }

    [TestMethod]
    public void Ticket_TotalAmount_IsConfiguredAsComputedColumn()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var property = context.Model
            .FindEntityType(typeof(Ticket))!
            .FindProperty(nameof(Ticket.TotalAmount));

        // Assert
        Assert.IsNotNull(property);

        Assert.IsFalse(
           string.IsNullOrWhiteSpace(property.GetComputedColumnSql()));

        Assert.AreEqual(
            "[Quantity] * [UnitPrice]",
            property.GetComputedColumnSql());
        
       
    }

    [TestMethod]
    public void Event_HasExpectedIndexes()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType = context.Model.FindEntityType(typeof(Event));

        // Assert
        Assert.IsNotNull(entityType);

        // Event currently has no explicit indexes.
        Assert.AreEqual(0, entityType.GetIndexes().Count());
    }

    [TestMethod]
    public void PricingTier_HasEventIdIndex()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType =
            context.Model.FindEntityType(typeof(PricingTier));

        // Assert
        Assert.IsNotNull(entityType);

        var index = entityType.GetIndexes()
            .SingleOrDefault(index =>
                index.Properties.Count == 1 &&
                index.Properties[0].Name == nameof(PricingTier.EventId));

        Assert.IsNotNull(index);
    }

    [TestMethod]
    public void Ticket_HasExpectedIndexes()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entityType =
            context.Model.FindEntityType(typeof(Ticket));

        // Assert
        Assert.IsNotNull(entityType);

        var indexes = entityType.GetIndexes();

        Assert.IsTrue(
            indexes.Any(index =>
                index.Properties.Count == 1 &&
                index.Properties[0].Name == nameof(Ticket.EventId)));

        Assert.IsTrue(
            indexes.Any(index =>
                index.Properties.Count == 1 &&
                index.Properties[0].Name == nameof(Ticket.PricingTierId)));

        Assert.IsTrue(
            indexes.Any(index =>
                index.Properties.Count == 1 &&
                index.Properties[0].Name == nameof(Ticket.PurchaseDate)));
    }
}

