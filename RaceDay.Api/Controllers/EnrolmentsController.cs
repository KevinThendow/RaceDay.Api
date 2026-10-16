
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Provides endpoints for creating and managing event enrolments.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        /// <summary>
        /// Initializes the enrolments controller.
        /// </summary>
        /// <param name="context">The RaceDay database context.</param>
        public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Converts an enrolment entity into an enrolment DTO.
        /// </summary>
        /// <param name="enrolment">The enrolment to convert.</param>
        /// <returns>The enrolment data transfer object.</returns>
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

        /// <summary>
        /// Enrols the logged-in participant in an event category.
        /// </summary>
        /// <param name="request">The event and category identifiers.</param>
        /// <returns>The newly created enrolment.</returns>
        /// <response code="201">Enrolment created successfully.</response>
        /// <response code="400">The selected category does not belong to the event.</response>
        /// <response code="401">The participant is not logged in.</response>
        /// <response code="403">The user is not a participant.</response>
        /// <response code="404">The event does not exist.</response>
        /// <response code="409">The participant is already enrolled in the event.</response>
        [HttpPost("enrolments")]
        [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateEnrolment(
            [FromBody] CreateEnrolmentRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "Please log in first." });

            if (role != "Participant")
                return StatusCode(StatusCodes.Status403Forbidden);

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

            return StatusCode(StatusCodes.Status201Created, ToDto(enrolment));
        }

        /// <summary>
        /// Retrieves all enrolments belonging to the logged-in participant.
        /// </summary>
        /// <returns>The participant's enrolments.</returns>
        /// <response code="200">Enrolments retrieved successfully.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not a participant.</response>
        [HttpGet("enrolments/mine")]
        [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized();

            if (role != "Participant")
                return StatusCode(StatusCodes.Status403Forbidden);

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantID == userId.Value)
                .ToListAsync();

            return Ok(enrolments.Select(ToDto));
        }

        /// <summary>
        /// Retrieves enrolments for an event owned by the logged-in organiser.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <returns>The event's enrolments.</returns>
        /// <response code="200">Enrolments retrieved successfully.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not authorised to view these enrolments.</response>
        /// <response code="404">The event does not exist.</response>
        [HttpGet("events/{eventId:int}/enrolments")]
        [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized();

            if (role != "Organiser")
                return StatusCode(StatusCodes.Status403Forbidden);

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            var enrolments = await _context.Enrolments
                .Where(e => e.EventID == eventId)
                .ToListAsync();

            return Ok(enrolments.Select(ToDto));
        }

        /// <summary>
        /// Updates an enrolment's status for an event owned by the organiser.
        /// </summary>
        /// <param name="id">The enrolment identifier.</param>
        /// <param name="request">The new enrolment status.</param>
        /// <returns>The updated enrolment.</returns>
        /// <response code="200">Enrolment status updated successfully.</response>
        /// <response code="400">The supplied status is invalid.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not authorised to update this enrolment.</response>
        /// <response code="404">The enrolment does not exist.</response>
        [HttpPut("enrolments/{id:int}/status")]
        [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateEnrolmentStatusRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized();

            if (role != "Organiser")
                return StatusCode(StatusCodes.Status403Forbidden);

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
                return StatusCode(StatusCodes.Status403Forbidden);

            enrolment.Status = request.Status;

            await _context.SaveChangesAsync();

            return Ok(ToDto(enrolment));
        }
    }
}
