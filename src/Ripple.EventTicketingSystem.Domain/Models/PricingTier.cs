using System;
using System.Collections.Generic;
using System.Text;

namespace Ripple.EventTicketingSystem.Domain.Models
{
    public class PricingTier
    {
        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Capacity { get; set; }

        public int AvailableQuantity { get; set; }

        public byte[] RowVersion { get; set; } = [];

        public Event Event { get; set; } = null!;

        public ICollection<Ticket> Tickets { get; set; }
            = new List<Ticket>();
    }
}
