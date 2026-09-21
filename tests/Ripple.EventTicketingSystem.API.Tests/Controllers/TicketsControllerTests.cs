using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ripple.EventTicketingSystem.API.Controllers;
using Ripple.EventTicketingSystem.Application.DTOs.Tickets;
using Ripple.EventTicketingSystem.Application.Interfaces;

namespace Ripple.EventTicketingSystem.API.Tests.Controllers
{
    [TestClass]
    public class TicketsControllerTests
    {
        private Mock<ITicketService> _ticketServiceMock = null!;
        private TicketsController _controller = null!;

        [TestInitialize]
        public void Setup()
        {
            _ticketServiceMock = new Mock<ITicketService>();

            _controller = new TicketsController(
                _ticketServiceMock.Object);
        }

        [TestMethod]
        public async Task GetAvailability_WhenServiceReturnsAvailability_ReturnsOk()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var cancellationToken = CancellationToken.None;

            var expectedResult = new EventAvailabilityResponse
            {
                EventId = eventId,
                EventName = "Music Festival",
                TotalCapacity = 1000,
                TotalAvailable = 850
            };

            _ticketServiceMock
                .Setup(x => x.GetAvailabilityAsync(
                    eventId,
                    cancellationToken))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAvailability(
                eventId,
                cancellationToken);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);

            var okResult = (OkObjectResult)result;

            Assert.AreSame(
                expectedResult,
                okResult.Value);

            _ticketServiceMock.Verify(
                x => x.GetAvailabilityAsync(
                    eventId,
                    cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task GetAvailability_WhenNoTicketsAvailable_ReturnsOk()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var cancellationToken = CancellationToken.None;

            var expectedResult = new EventAvailabilityResponse
            {
                EventId = eventId,
                EventName = "Sold Out Music Festival",
                TotalCapacity = 1000,
                TotalAvailable = 0
            };

            _ticketServiceMock
                .Setup(x => x.GetAvailabilityAsync(
                    eventId,
                    cancellationToken))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAvailability(
                eventId,
                cancellationToken);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);

            var okResult = (OkObjectResult)result;

            Assert.AreSame(
                expectedResult,
                okResult.Value);

            _ticketServiceMock.Verify(
                x => x.GetAvailabilityAsync(
                    eventId,
                    cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task Purchase_WhenServiceReturnsTicket_ReturnsCreated()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();
            var pricingTierId = Guid.NewGuid();
            var cancellationToken = CancellationToken.None;

            var request = new PurchaseTicketRequest
            {
                PricingTierId = pricingTierId,
                Quantity = 2,
                CustomerName = "John Smith",
                CustomerEmail = "john.smith@example.com"
            };

            var expectedResult = new TicketResponse
            {
                Id = ticketId,
                EventId = eventId,
                PricingTierId = pricingTierId,
                PricingTierName = "General Admission",
                Quantity = 2,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                UnitPrice = 100m,
                TotalAmount = 200m,
                PurchaseDate = DateTime.UtcNow
            };

            _ticketServiceMock
                .Setup(x => x.PurchaseAsync(
                    eventId,
                    request,
                    cancellationToken))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.Purchase(
                eventId,
                request,
                cancellationToken);

            // Assert
            Assert.IsInstanceOfType<ObjectResult>(result);

            var objectResult = (ObjectResult)result;

            Assert.AreEqual(
                StatusCodes.Status201Created,
                objectResult.StatusCode);

            Assert.AreSame(
                expectedResult,
                objectResult.Value);

            _ticketServiceMock.Verify(
                x => x.PurchaseAsync(
                    eventId,
                    request,
                    cancellationToken),
                Times.Once);
        }
    }
}

