
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Provides endpoints for browsing and managing RaceDay events.
    /// </summary>
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        /// <summary>
        /// Initialises the event controller.
        /// </summary>
        /// <param name="context">The RaceDay database context.</param>
        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all available events.
        /// </summary>
        /// <returns>A list of events.</returns>
        /// <response code="200">Events retrieved successfully.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .Select(e => new EventDto
                {
                    EventId = e.EventID,
                    EventName = e.EventName,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    Distance = e.Distance,
                    EventType = e.EventType,
                    BannerImageUrl = e.BannerImageUrl,
                    OrganiserId = e.OrganiserID
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>
        /// Retrieves a specific event using its ID.
        /// </summary>
        /// <param name="id">The ID of the event.</param>
        /// <returns>The requested event.</returns>
        /// <response code="200">Event retrieved successfully.</response>
        /// <response code="404">The event was not found.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _context.Events
                .Where(e => e.EventID == id)
                .Select(e => new EventDto
                {
                    EventId = e.EventID,
                    EventName = e.EventName,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    Distance = e.Distance,
                    EventType = e.EventType,
                    BannerImageUrl = e.BannerImageUrl,
                    OrganiserId = e.OrganiserID
                })
                .FirstOrDefaultAsync();

            if (ev == null)
                return NotFound();

            return Ok(ev);
        }

        /// <summary>
        /// Retrieves events managed by the currently logged-in organiser.
        /// </summary>
        /// <returns>The organiser's events.</returns>
        /// <response code="200">Events retrieved successfully.</response>
        /// <response code="401">The user is not logged in.</response>
        [HttpGet("mine")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyEvents()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Unauthorized();

            var events = await _context.Events
                .Where(e => e.OrganiserID == userId.Value)
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>
        /// Creates a new event for the logged-in organiser.
        /// </summary>
        /// <param name="request">The event details.</param>
        /// <returns>The ID of the newly created event.</returns>
        /// <response code="201">Event created successfully.</response>
        /// <response code="400">The event details are invalid.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not an organiser.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent(
            [FromBody] CreateEventRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized();

            if (role != "Organiser")
                return StatusCode(403, "Only organisers can create events.");

            if (string.IsNullOrWhiteSpace(request.EventName) ||
                string.IsNullOrWhiteSpace(request.Location) ||
                request.Distance <= 0)
                return BadRequest("Valid event name, location and distance are required.");

            var ev = new RaceDay.Api.Models.Event
            {
                OrganiserID = userId.Value,
                EventName = request.EventName.Trim(),
                Description = request.Description,
                EventDate = request.EventDate,
                Location = request.Location.Trim(),
                Distance = request.Distance,
                EventType = request.EventType,
                BannerImageUrl = request.BannerImageUrl,
                CreatedAt = DateTime.Now
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Event created successfully.",
                eventId = ev.EventID
            });
        }

        /// <summary>
        /// Updates an existing event owned by the logged-in organiser.
        /// </summary>
        /// <param name="id">The ID of the event to update.</param>
        /// <param name="request">The updated event details.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Event updated successfully.</response>
        /// <response code="400">The event details are invalid.</response>
        /// <response code="403">The user is not authorised to update the event.</response>
        /// <response code="404">The event was not found.</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(
            int id, [FromBody] UpdateEventRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null || role != "Organiser")
                return Forbid();

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null)
                return NotFound();

            if (ev.OrganiserID != userId.Value)
                return Forbid();

            if (string.IsNullOrWhiteSpace(request.EventName) ||
                string.IsNullOrWhiteSpace(request.Location) ||
                request.Distance <= 0)
                return BadRequest("Valid event name, location and distance are required.");

            ev.EventName = request.EventName.Trim();
            ev.Description = request.Description;
            ev.EventDate = request.EventDate;
            ev.Location = request.Location.Trim();
            ev.Distance = request.Distance;
            ev.EventType = request.EventType;
            ev.BannerImageUrl = request.BannerImageUrl;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Event updated successfully." });
        }

        /// <summary>
        /// Deletes an event owned by the logged-in organiser.
        /// </summary>
        /// <param name="id">The ID of the event to delete.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Event deleted successfully.</response>
        /// <response code="403">The user is not authorised to delete the event.</response>
        /// <response code="404">The event was not found.</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null || role != "Organiser")
                return Forbid();

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null)
                return NotFound();

            if (ev.OrganiserID != userId.Value)
                return Forbid();

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Event deleted successfully." });
        }
    }
}
