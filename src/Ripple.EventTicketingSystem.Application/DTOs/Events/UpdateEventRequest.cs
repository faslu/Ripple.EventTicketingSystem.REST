using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
namespace Ripple.EventTicketingSystem.Application.DTOs.Events
{

    public class UpdateEventRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(300)]
        public string Venue { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public TimeSpan EventTime { get; set; }

        [Range(1, int.MaxValue)]
        public int TotalCapacity { get; set; }
    }
}
