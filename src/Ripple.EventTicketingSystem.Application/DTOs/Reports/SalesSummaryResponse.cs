using System;
using System.Collections.Generic;
using System.Text;

namespace Ripple.EventTicketingSystem.Application.DTOs.Reports
{
    public class SalesSummaryResponse
    {
        public Guid EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public int TicketsSold { get; set; }

        public decimal Revenue { get; set; }
    }
}
