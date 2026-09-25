using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ripple.EventTicketingSystem.Application.DTOs.Reports;
using Ripple.EventTicketingSystem.Application.Interfaces;
using Ripple.EventTicketingSystem.Application.Services;
using static System.Net.Mime.MediaTypeNames;

namespace Ripple.EventTicketingSystem.Application.Tests.Services
{
    [TestClass]
    public class ReportServiceTests
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IEventRepository> _eventRepository = null!;
        private ReportService _service = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _eventRepository = new Mock<IEventRepository>();

            _unitOfWork
                .SetupGet(x => x.Events)
                .Returns(_eventRepository.Object);

            _service = new ReportService(_unitOfWork.Object);
        }

        [TestMethod]
        public async Task GetSalesSummaryAsync_WhenRepositoryReturnsEmptyList_ReturnsEmptyList()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            _eventRepository
                .Setup(x => x.GetSalesSummaryAsync(cancellationToken))
                .ReturnsAsync(new List<SalesSummaryResponse>());

            // Act
            var result = await _service.GetSalesSummaryAsync(
                cancellationToken);

            // Assert
            Assert.IsNotNull(result);
            Assert.HasCount(0, result);

            _eventRepository.Verify(
                x => x.GetSalesSummaryAsync(cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task GetSalesSummaryAsync_WhenRepositoryReturnsResults_ReturnsResults()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            var expected = new List<SalesSummaryResponse>
            {
                new SalesSummaryResponse
                {
                    EventName = "Music Fest",
                    TicketsSold = 5,
                    Revenue = 650
                }
            };

            _eventRepository
                .Setup(x => x.GetSalesSummaryAsync(cancellationToken))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.GetSalesSummaryAsync(
                cancellationToken);

            // Assert
            Assert.IsNotNull(result);
            Assert.HasCount(1, result);

            Assert.AreEqual("Music Fest", result[0].EventName);
            Assert.AreEqual(5, result[0].TicketsSold);
            Assert.AreEqual(650, result[0].Revenue);

            _eventRepository.Verify(
                x => x.GetSalesSummaryAsync(cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task GetSalesSummaryAsync_WhenRepositoryReturnsMultipleResults_ReturnsSameResults()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            var expected = new List<SalesSummaryResponse>
            {
                new SalesSummaryResponse
                {
                    EventName = "Event B",
                    TicketsSold = 5,
                    Revenue = 500
                },
                new SalesSummaryResponse
                {
                    EventName = "Event A",
                    TicketsSold = 1,
                    Revenue = 100
                }
            };

            _eventRepository
                .Setup(x => x.GetSalesSummaryAsync(cancellationToken))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.GetSalesSummaryAsync(
                cancellationToken);

            // Assert
            Assert.HasCount(2, result);

            Assert.AreEqual("Event B", result[0].EventName);
            Assert.AreEqual(5, result[0].TicketsSold);
            Assert.AreEqual(500, result[0].Revenue);

            Assert.AreEqual("Event A", result[1].EventName);
            Assert.AreEqual(1, result[1].TicketsSold);
            Assert.AreEqual(100, result[1].Revenue);

            _eventRepository.Verify(
                x => x.GetSalesSummaryAsync(cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task GetSalesSummaryAsync_PassesCancellationTokenToRepository()
        {
            // Arrange
            using var cancellationTokenSource =
                new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            var expected = new List<SalesSummaryResponse>();

            _eventRepository
                .Setup(x => x.GetSalesSummaryAsync(cancellationToken))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.GetSalesSummaryAsync(
                cancellationToken);

            // Assert
            Assert.IsNotNull(result);

            _eventRepository.Verify(
                x => x.GetSalesSummaryAsync(cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task GetSalesSummaryAsync_WhenRepositoryReturnsList_ReturnsSameList()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            var expected = new List<SalesSummaryResponse>
            {
                new SalesSummaryResponse
                {
                    EventName = "Tech Expo",
                    TicketsSold = 10,
                    Revenue = 1000
                }
            };

            _eventRepository
                .Setup(x => x.GetSalesSummaryAsync(cancellationToken))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.GetSalesSummaryAsync(
                cancellationToken);

            // Assert
            Assert.AreSame(expected, result);
        }
    }
}
