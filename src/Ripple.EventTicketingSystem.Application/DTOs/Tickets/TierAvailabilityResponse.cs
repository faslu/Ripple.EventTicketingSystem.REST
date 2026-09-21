using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Ripple.EventTicketingSystem.Application.DTOs.Tickets
{
    
    public class TierAvailabilityResponse
    {
        public Guid PricingTierId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Capacity { get; set; }

        public int AvailableQuantity { get; set; }
    }
}
