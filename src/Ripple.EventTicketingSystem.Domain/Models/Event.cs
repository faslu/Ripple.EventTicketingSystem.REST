using System;
using System.Collections.Generic;
using System.Text;

namespace Ripple.EventTicketingSystem.Domain.Models
{
    public class Event
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Venue { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public TimeSpan EventTime { get; set; }

        public int TotalCapacity { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public ICollection<PricingTier> PricingTiers { get; set; }
            = new List<PricingTier>();

        public ICollection<Ticket> Tickets { get; set; }
            = new List<Ticket>();
    }
}
