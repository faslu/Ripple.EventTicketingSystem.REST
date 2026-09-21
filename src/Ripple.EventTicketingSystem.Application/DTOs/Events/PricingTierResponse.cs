using System;
using System.Collections.Generic;
using System.Text;

namespace Ripple.EventTicketingSystem.Application.DTOs.Events
{
   
    public class PricingTierResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Capacity { get; set; }

        public int AvailableQuantity { get; set; }
    }
}
