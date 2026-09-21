using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Ripple.EventTicketingSystem.Application.DTOs.Events
{
    public class CreatePricingTierRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 99999999)]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }
    }
}
