using System;
using System.Collections.Generic;
using System.Text;

namespace Ripple.EventTicketingSystem.Application.DTOs.Tickets
{
    
    public class TicketResponse
    {
        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        public Guid PricingTierId { get; set; }

        public string PricingTierName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime PurchaseDate { get; set; }
    }
}
