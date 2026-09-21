using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ripple.EventTicketingSystem.Application.DTOs.Events;
using Ripple.EventTicketingSystem.Application.Interfaces;


namespace Ripple.EventTicketingSystem.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create(
            CreateEventRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _eventService.CreateAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var result = await _eventService.GetAllAsync(
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _eventService.GetByIdAsync(
                id,
                cancellationToken);

            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(
            Guid id,
            UpdateEventRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _eventService.UpdateAsync(
                id,
                request,
                cancellationToken);

            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _eventService.DeleteAsync(
                id,
                cancellationToken);

            return NoContent();
        }
    }
}
