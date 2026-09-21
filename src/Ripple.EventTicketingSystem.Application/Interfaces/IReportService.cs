using System;
using System.Collections.Generic;
using System.Text;
using Ripple.EventTicketingSystem.Application.DTOs.Reports;

namespace Ripple.EventTicketingSystem.Application.Interfaces
{

    public interface IReportService
    {
        Task<List<SalesSummaryResponse>> GetSalesSummaryAsync(
            CancellationToken cancellationToken);
    }
}
