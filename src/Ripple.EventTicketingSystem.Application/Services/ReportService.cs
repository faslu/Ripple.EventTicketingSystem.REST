using Ripple.EventTicketingSystem.Application.DTOs.Reports;
using Ripple.EventTicketingSystem.Application.Interfaces;

namespace Ripple.EventTicketingSystem.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<SalesSummaryResponse>> GetSalesSummaryAsync(
            CancellationToken cancellationToken)
        {
            return await _unitOfWork.Events
                .GetSalesSummaryAsync(cancellationToken);
        }
    }
}