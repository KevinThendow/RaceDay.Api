
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private EnrolmentDto ToDto(Enrolment enrolment)
        {
            return new EnrolmentDto
            {
                EnrolmentId = enrolment.EnrolmentID,
                ParticipantId = enrolment.ParticipantID,
                EventId = enrolment.EventID,
                CategoryId = enrolment.CategoryID,
                EnrolmentDate = enrolment.EnrolmentDate,
                Status = enrolment.Status
            };
        }

        // POST: /api/enrolments
        [HttpPost("enrolments")]
        public async Task<IActionResult> CreateEnrolment(
            [FromBody] CreateEnrolmentRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "Please log in first." });

            if (role != "Participant")
                return Forbid();

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == request.EventId);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.CategoryID == request.CategoryId &&
                    c.EventID == request.EventId);

            if (category == null)
            {
                return BadRequest(new
                {
                    message = "The selected category does not belong to this event."
                });
            }

            bool alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e =>
                    e.ParticipantID == userId.Value &&
                    e.EventID == request.EventId);

            if (alreadyEnrolled)
            {
                return Conflict(new
                {
                    message = "You are already enrolled in this event."
                });
            }

            var enrolment = new Enrolment
            {
                ParticipantID = userId.Value,
                EventID = request.EventId,
                CategoryID = request.CategoryId,
                EnrolmentDate = DateTime.Now,
                Status = "Pending"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return StatusCode(201, ToDto(enrolment));
        }

        // GET: /api/enrolments/mine
        [HttpGet("enrolments/mine")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized();

            if (role != "Participant")
                return Forbid();

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantID == userId.Value)
                .ToListAsync();

            return Ok(enrolments.Select(ToDto));
        }

        // GET: /api/events/{eventId}/enrolments
        [HttpGet("events/{eventId:int}/enrolments")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized();

            if (role != "Organiser")
                return Forbid();

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganiserID != userId.Value)
                return Forbid();

            var enrolments = await _context.Enrolments
                .Where(e => e.EventID == eventId)
                .ToListAsync();

            return Ok(enrolments.Select(ToDto));
        }

        // PUT: /api/enrolments/{id}/status
        [HttpPut("enrolments/{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateEnrolmentStatusRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized();

            if (role != "Organiser")
                return Forbid();

            if (request.Status != "Pending" &&
                request.Status != "Confirmed" &&
                request.Status != "Cancelled")
            {
                return BadRequest(new
                {
                    message = "Status must be Pending, Confirmed or Cancelled."
                });
            }

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);

            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found." });

            if (enrolment.Event == null ||
                enrolment.Event.OrganiserID != userId.Value)
                return Forbid();

            enrolment.Status = request.Status;

            await _context.SaveChangesAsync();

            return Ok(ToDto(enrolment));
        }
    }
}
