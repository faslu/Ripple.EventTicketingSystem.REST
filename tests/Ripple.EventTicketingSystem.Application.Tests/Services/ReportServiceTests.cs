using Microsoft.VisualStudio.TestTools.UnitTesting;
using MockQueryable.Moq;
using Moq;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Application.Services;
using Ripple.EventTicketingSystem.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ripple.EventTicketingSystem.Application.Tests.Services
{
    [TestClass]
    public class ReportServiceTests
    {
        [TestMethod]
        public async Task GetSalesSummaryAsync_WhenNoEvents_ReturnsEmptyList()
        {
            // Arrange
            var mockDb = new Mock<ITicketingDbContext>();
            mockDb.Setup(x => x.Events)
                  .Returns(new List<Event>().BuildMockDbSet().Object);

            var service = new ReportService(mockDb.Object);

            // Act
            var result = await service.GetSalesSummaryAsync(CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.HasCount(0, result);
        }

        [TestMethod]
        public async Task GetSalesSummaryAsync_WhenEventsHaveNoTickets_ReturnsZeroSales()
        {
            // Arrange
            var events = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Name = "Tech Expo",
                    Tickets = new List<Ticket>() // No tickets
                }
            }.BuildMockDbSet();

            var mockDb = new Mock<ITicketingDbContext>();
            mockDb.Setup(x => x.Events).Returns(events.Object);

            var service = new ReportService(mockDb.Object);

            // Act
            var result = await service.GetSalesSummaryAsync(CancellationToken.None);

            // Assert
            Assert.HasCount(1, result);
            Assert.AreEqual(0, result[0].TicketsSold);
            Assert.AreEqual(0, result[0].Revenue);
        }

        [TestMethod]
        public async Task GetSalesSummaryAsync_WhenEventsHaveTickets_ComputesCorrectTotals()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            var events = new List<Event>
            {
                new Event
                {
                    Id = eventId,
                    Name = "Music Fest",
                    Tickets = new List<Ticket>
                    {
                        new Ticket (2, 100),
                        new Ticket(3, 150)
                    }
                }
            }.BuildMockDbSet();

            var mockDb = new Mock<ITicketingDbContext>();
            mockDb.Setup(x => x.Events).Returns(events.Object);

            var service = new ReportService(mockDb.Object);

            // Act
            var result = await service.GetSalesSummaryAsync(CancellationToken.None);

            // Assert
            Assert.HasCount(1, result);
            Assert.AreEqual(5, result[0].TicketsSold);      // 2 + 3
            Assert.AreEqual(650, result[0].Revenue);        // 200 + 450
        }

        [TestMethod]
        public async Task GetSalesSummaryAsync_WhenMultipleEvents_ReturnsOrderedByRevenue()
        {
            // Arrange
            var events = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Name = "Event A",
                    Tickets = new List<Ticket>
                    {
                        new Ticket(1, 100)
                    }
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Name = "Event B",
                    Tickets = new List<Ticket>
                    {
                        new Ticket(5, 100)
                    }
                }
            }.BuildMockDbSet();

            var mockDb = new Mock<ITicketingDbContext>();
            mockDb.Setup(x => x.Events).Returns(events.Object);

            var service = new ReportService(mockDb.Object);

            // Act
            var result = await service.GetSalesSummaryAsync(CancellationToken.None);

            // Assert
            Assert.HasCount(2, result);

            // Event B should come first (higher revenue)
            Assert.AreEqual("Event B", result[0].EventName);
            Assert.AreEqual(500, result[0].Revenue);

            Assert.AreEqual("Event A", result[1].EventName);
            Assert.AreEqual(100, result[1].Revenue);
        }
    }
}
