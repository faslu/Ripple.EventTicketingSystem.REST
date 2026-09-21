using System;
using System.Collections.Generic;
using System.Text;

namespace Ripple.EventTicketingSystem.Application.DTOs.Tickets
{

    public class EventAvailabilityResponse
    {
        public Guid EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public int TotalCapacity { get; set; }

        public int TotalAvailable { get; set; }

        public List<TierAvailabilityResponse> PricingTiers { get; set; }
            = [];
    }
}
