using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ripple.EventTicketingSystem.Application.DTOs.Reports;
using Ripple.EventTicketingSystem.Application.Interfaces;



namespace Ripple.EventTicketingSystem.Application.Services
{
   
    public class ReportService : IReportService
    {
        private readonly ITicketingDbContext _db;

        public ReportService(ITicketingDbContext db)
        {
            _db = db;
        }

        public async Task<List<SalesSummaryResponse>> GetSalesSummaryAsync(
            CancellationToken cancellationToken)
        {
            return await _db.Events
                .AsNoTracking()
                .Select(e => new SalesSummaryResponse
                {
                    EventId = e.Id,
                    EventName = e.Name,

                    TicketsSold = e.Tickets
                        .Select(t => (int?)t.Quantity)
                        .Sum() ?? 0,

                    Revenue = e.Tickets
                        .Select(t => (decimal?)t.TotalAmount)
                        .Sum() ?? 0
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync(cancellationToken);
        }
    }
}
