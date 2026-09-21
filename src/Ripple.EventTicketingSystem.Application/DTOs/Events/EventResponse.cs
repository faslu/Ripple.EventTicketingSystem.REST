using System;
using System.Collections.Generic;
using System.Text;

namespace Ripple.EventTicketingSystem.Application.DTOs.Events
{   

    public class EventResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Venue { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public TimeSpan EventTime { get; set; }

        public int TotalCapacity { get; set; }

        public List<PricingTierResponse> PricingTiers { get; set; }
            = [];
    }
}
