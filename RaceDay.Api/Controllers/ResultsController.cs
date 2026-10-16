
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private ResultDto ToDto(Result result)
        {
            return new ResultDto
            {
                ResultId = result.ResultID,
                EnrolmentId = result.EnrolmentID,
                FinishTime = result.FinishTime,
                FinishingPosition = result.FinishingPosition,
                RecordedAt = result.RecordedAt
            };
        }

        // POST: /api/results
        [HttpPost("results")]
        public async Task<IActionResult> CreateResult(
            [FromBody] CreateResultRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "Please log in first." });

            if (role != "Organiser")
                return Forbid();

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(
                    e => e.EnrolmentID == request.EnrolmentId);

            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found." });

            if (enrolment.Event == null ||
                enrolment.Event.OrganiserID != userId.Value)
                return Forbid();

            if (enrolment.Status != "Confirmed")
            {
                return BadRequest(new
                {
                    message = "Only confirmed enrolments can receive results."
                });
            }

            if (request.FinishingPosition.HasValue &&
                request.FinishingPosition.Value < 1)
            {
                return BadRequest(new
                {
                    message = "Finishing position must be positive."
                });
            }

            bool resultExists = await _context.Results
                .AnyAsync(r => r.EnrolmentID == request.EnrolmentId);

            if (resultExists)
            {
                return Conflict(new
                {
                    message = "A result already exists for this enrolment."
                });
            }

            var result = new Result
            {
                EnrolmentID = request.EnrolmentId,
                FinishTime = request.FinishTime,
                FinishingPosition = request.FinishingPosition,
                RecordedAt = DateTime.Now
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return StatusCode(201, ToDto(result));
        }

        // GET: /api/results/mine
        [HttpGet("results/mine")]
        public async Task<IActionResult> GetMyResults()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized();

            if (role != "Participant")
                return Forbid();

            var results = await _context.Results
                .Where(r => r.Enrolment != null &&
                            r.Enrolment.ParticipantID == userId.Value)
                .ToListAsync();

            return Ok(results.Select(ToDto));
        }

        // GET: /api/events/{eventId}/results
        [HttpGet("events/{eventId:int}/results")]
        public async Task<IActionResult> GetEventResults(int eventId)
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

            var results = await _context.Results
                .Where(r => r.Enrolment != null &&
                            r.Enrolment.EventID == eventId)
                .ToListAsync();

            return Ok(results.Select(ToDto));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateResult(
        int id,
        [FromBody] UpdateResultRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized();

            if (role != "Organiser")
                return Forbid();

            var result = await _context.Results
                .Include(r => r.Enrolment)
                .ThenInclude(e => e.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
                return NotFound("Result not found.");

            if (result.Enrolment.Event.OrganiserID != userId.Value)
                return Forbid();

            if (request.FinishingPosition.HasValue &&
                request.FinishingPosition.Value <= 0)
            {
                return BadRequest(
                    "Finishing position must be greater than zero.");
            }

            result.FinishTime = request.FinishTime;
            result.FinishingPosition = request.FinishingPosition;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Result updated successfully.",
                resultId = result.ResultID
            });
        }
    }
}
