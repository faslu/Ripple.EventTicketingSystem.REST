using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ripple.EventTicketingSystem.API.Controllers;
using Ripple.EventTicketingSystem.Application.DTOs.Reports;
using Ripple.EventTicketingSystem.Application.Interfaces;

namespace Ripple.EventTicketingSystem.API.Tests.Controllers
{
    [TestClass]
    public class ReportsControllerTests
    {
        private Mock<IReportService> _reportServiceMock = null!;
        private ReportsController _controller = null!;

        [TestInitialize]
        public void Setup()
        {
            _reportServiceMock = new Mock<IReportService>();
            _controller = new ReportsController(
                _reportServiceMock.Object);
        }

        [TestMethod]
        public async Task GetSalesSummary_WhenServiceReturnsData_ReturnsOk()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            var expectedResult = new List<SalesSummaryResponse>
            {
                new SalesSummaryResponse
                {
                    EventId = Guid.NewGuid(),
                    EventName = "Music Festival",
                    TicketsSold = 100,
                    Revenue = 10000m
                },
                new SalesSummaryResponse
                {
                    EventId = Guid.NewGuid(),
                    EventName = "Tech Conference",
                    TicketsSold = 50,
                    Revenue = 7500m
                }
            };

            _reportServiceMock
                .Setup(x => x.GetSalesSummaryAsync(
                    cancellationToken))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetSalesSummary(
                cancellationToken);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);

            var okResult = (OkObjectResult)result;

            Assert.AreSame(
                expectedResult,
                okResult.Value);

            _reportServiceMock.Verify(
                x => x.GetSalesSummaryAsync(
                    cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task GetSalesSummary_WhenServiceReturnsEmptyList_ReturnsOk()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            var expectedResult = new List<SalesSummaryResponse>();

            _reportServiceMock
                .Setup(x => x.GetSalesSummaryAsync(
                    cancellationToken))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetSalesSummary(
                cancellationToken);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);

            var okResult = (OkObjectResult)result;

            Assert.IsNotNull(okResult.Value);

            Assert.AreSame(
                expectedResult,
                okResult.Value);

            _reportServiceMock.Verify(
                x => x.GetSalesSummaryAsync(
                    cancellationToken),
                Times.Once);
        }
    }
}

