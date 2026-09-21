using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Ripple.EventTicketingSystem.Application.DTOs.Tickets
{
  
    public class PurchaseTicketRequest
    {
        [Required]
        public Guid PricingTierId { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; }

        [Required]
        [MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(320)]
        public string CustomerEmail { get; set; } = string.Empty;
    }
}
