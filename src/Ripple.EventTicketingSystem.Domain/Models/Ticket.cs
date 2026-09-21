using System;
using System.Collections.Generic;
using System.Text;

namespace Ripple.EventTicketingSystem.Domain.Models
{
    public class Ticket
    {
        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        public Guid PricingTierId { get; set; }

        public int Quantity { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        public decimal TotalAmount { get; private set; }

        public DateTime PurchaseDate { get; set; }

        public Event Event { get; set; } = null!;

        public PricingTier PricingTier { get; set; } = null!;

        public Ticket()
        {
        }

        public Ticket(int quantity, decimal unitPrice)
        {
            Quantity = quantity;
            UnitPrice = unitPrice;
            TotalAmount = quantity * unitPrice;
        }
    }
}
