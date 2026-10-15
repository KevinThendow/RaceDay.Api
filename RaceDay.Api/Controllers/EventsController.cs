
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        [HttpGet]
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

        [HttpGet("{id:int}")]
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

        [HttpGet("mine")]
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

        [HttpPost]
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

        [HttpPut("{id:int}")]
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

        [HttpDelete("{id:int}")]
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
