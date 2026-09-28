using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ripple.EventTicketingSystem.Application.DTOs.Tickets;
using Ripple.EventTicketingSystem.Application.Interfaces;

namespace Ripple.EventTicketingSystem.API.Controllers
{
    [Route("api/events/{eventId:guid}")]
    [ApiController]    
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpGet("availability")]
        [ProducesResponseType(
            typeof(EventAvailabilityResponse),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailability(
            Guid eventId,
            CancellationToken cancellationToken)
        {
            var result = await _ticketService.GetAvailabilityAsync(
                eventId,
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("tickets")]
        [ProducesResponseType(
            typeof(TicketResponse),
            StatusCodes.Status201Created)]
        public async Task<IActionResult> Purchase(
            Guid eventId,
            PurchaseTicketRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _ticketService.PurchaseAsync(
                eventId,
                request,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }
    }
}
