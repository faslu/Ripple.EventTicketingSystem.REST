
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ripple.EventTicketingSystem.API.Controllers;
using Ripple.EventTicketingSystem.Application.DTOs.Events;
using Ripple.EventTicketingSystem.Application.Interfaces;
using System.Timers;

namespace Ripple.EventTicketingSystem.API.Tests.Controllers
{
    [TestClass]
    public class EventsControllerTests
    {
        private Mock<IEventService> _eventServiceMock = null!;
        private EventsController _controller = null!;

        [TestInitialize]
        public void Setup()
        {
            _eventServiceMock = new Mock<IEventService>();
            _controller = new EventsController(_eventServiceMock.Object);
        }

        [TestMethod]
        public async Task Create_WhenServiceReturnsEvent_ReturnsCreatedAtAction()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var pricingTierId = Guid.NewGuid();
            var cancellationToken = CancellationToken.None;

            var request = new CreateEventRequest
            {
                Name = "Music Festival",
                Description = "Annual music festival",
                Venue = "Sydney Showground",
                EventDate = DateTime.UtcNow.Date.AddDays(30),
                EventTime = new TimeSpan(18, 0, 0),
                TotalCapacity = 1000,
                PricingTiers =
                [
                    new CreatePricingTierRequest
                    {
                        Name = "General Admission",
                        Price = 100m,
                        Capacity = 1000
                    }
                ]
            };

            var expectedResponse = new EventResponse
            {
                Id = eventId,
                Name = request.Name,
                Description = request.Description,
                Venue = request.Venue,
                EventDate = request.EventDate,
                EventTime = request.EventTime,
                TotalCapacity = request.TotalCapacity,
                PricingTiers =
                [
                    new PricingTierResponse
                    {
                        Id = pricingTierId,
                        Name = "General Admission",
                        Price = 100m,
                        Capacity = 1000,
                        AvailableQuantity = 1000
                    }
                ]
            };

            _eventServiceMock
                .Setup(x => x.CreateAsync(
                    request,
                    cancellationToken))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Create(
                request,
                cancellationToken);

            // Assert
            Assert.IsInstanceOfType<CreatedAtActionResult>(result);

            var createdResult = (CreatedAtActionResult)result;

            Assert.AreEqual(
                nameof(EventsController.GetById),
                createdResult.ActionName);

            Assert.IsNotNull(createdResult.RouteValues);

            Assert.AreEqual(
                eventId,
                createdResult.RouteValues["id"]);

            Assert.AreSame(
                expectedResponse,
                createdResult.Value);

            _eventServiceMock.Verify(
                x => x.CreateAsync(
                    request,
                    cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task GetAll_WhenServiceReturnsEvents_ReturnsOk()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            var expectedEvents = new List<EventResponse>
            {
                new EventResponse
                {
                    Id = Guid.NewGuid(),
                    Name = "Music Festival",
                    Description = "Annual music festival",
                    Venue = "Sydney Showground",
                    EventDate = DateTime.UtcNow.Date.AddDays(30),
                    EventTime = new TimeSpan(18, 0, 0),
                    TotalCapacity = 1000,
                    PricingTiers =
                    [
                        new PricingTierResponse
                        {
                            Id = Guid.NewGuid(),
                            Name = "General Admission",
                            Price = 100m,
                            Capacity = 1000,
                            AvailableQuantity = 1000
                        }
                    ]
                },
                new EventResponse
                {
                    Id = Guid.NewGuid(),
                    Name = "Tech Conference",
                    Description = "Technology conference",
                    Venue = "ICC Sydney",
                    EventDate = DateTime.UtcNow.Date.AddDays(60),
                    EventTime = new TimeSpan(9, 0, 0),
                    TotalCapacity = 500,
                    PricingTiers =
                    [
                        new PricingTierResponse
                        {
                            Id = Guid.NewGuid(),
                            Name = "Standard",
                            Price = 150m,
                            Capacity = 500,
                            AvailableQuantity = 500
                        }
                    ]
                }
            };

            _eventServiceMock
                .Setup(x => x.GetAllAsync(cancellationToken))
                .ReturnsAsync(expectedEvents);

            // Act
            var result = await _controller.GetAll(
                cancellationToken);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);

            var okResult = (OkObjectResult)result;

            Assert.AreSame(
                expectedEvents,
                okResult.Value);

            _eventServiceMock.Verify(
                x => x.GetAllAsync(cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task GetById_WhenServiceReturnsEvent_ReturnsOk()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var cancellationToken = CancellationToken.None;

            var expectedResponse = new EventResponse
            {
                Id = eventId,
                Name = "Music Festival",
                Description = "Annual music festival",
                Venue = "Sydney Showground",
                EventDate = DateTime.UtcNow.Date.AddDays(30),
                EventTime = new TimeSpan(18, 0, 0),
                TotalCapacity = 1000,
                PricingTiers =
                [
                    new PricingTierResponse
                    {
                        Id = Guid.NewGuid(),
                        Name = "General Admission",
                        Price = 100m,
                        Capacity = 1000,
                        AvailableQuantity = 1000
                    }
                ]
            };

            _eventServiceMock
                .Setup(x => x.GetByIdAsync(
                    eventId,
                    cancellationToken))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetById(
                eventId,
                cancellationToken);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);

            var okResult = (OkObjectResult)result;

            Assert.AreSame(
                expectedResponse,
                okResult.Value);

            _eventServiceMock.Verify(
                x => x.GetByIdAsync(
                    eventId,
                    cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task Update_WhenServiceReturnsEvent_ReturnsOk()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var cancellationToken = CancellationToken.None;

            var request = new UpdateEventRequest
            {
                Name = "Updated Music Festival",
                Description = "Updated description",
                Venue = "Updated Venue",
                EventDate = DateTime.UtcNow.Date.AddDays(45),
                EventTime = new TimeSpan(19, 0, 0),
                TotalCapacity = 1500
            };

            var expectedResponse = new EventResponse
            {
                Id = eventId,
                Name = request.Name,
                Description = request.Description,
                Venue = request.Venue,
                EventDate = request.EventDate,
                EventTime = request.EventTime,
                TotalCapacity = request.TotalCapacity,
                PricingTiers =
                [
                    new PricingTierResponse
                    {
                        Id = Guid.NewGuid(),
                        Name = "General Admission",
                        Price = 100m,
                        Capacity = 1500,
                        AvailableQuantity = 1500
                    }
                ]
            };

            _eventServiceMock
                .Setup(x => x.UpdateAsync(
                    eventId,
                    request,
                    cancellationToken))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Update(
                eventId,
                request,
                cancellationToken);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);

            var okResult = (OkObjectResult)result;

            Assert.AreSame(
                expectedResponse,
                okResult.Value);

            _eventServiceMock.Verify(
                x => x.UpdateAsync(
                    eventId,
                    request,
                    cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task Delete_WhenServiceCompletes_ReturnsNoContent()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var cancellationToken = CancellationToken.None;

            _eventServiceMock
                .Setup(x => x.DeleteAsync(
                    eventId,
                    cancellationToken))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(
                eventId,
                cancellationToken);

            // Assert
            Assert.IsInstanceOfType<NoContentResult>(result);

            _eventServiceMock.Verify(
                x => x.DeleteAsync(
                    eventId,
                    cancellationToken),
                Times.Once);
        }
    }
}

