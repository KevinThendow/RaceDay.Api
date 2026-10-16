
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Provides endpoints for recording, viewing and updating event results.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        /// <summary>
        /// Initializes the results controller.
        /// </summary>
        /// <param name="context">The RaceDay database context.</param>
        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Converts a result entity into a result DTO.
        /// </summary>
        /// <param name="result">The result to convert.</param>
        /// <returns>The result data transfer object.</returns>
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

        /// <summary>
        /// Records a result for a confirmed enrolment in an event
        /// owned by the logged-in organiser.
        /// </summary>
        /// <param name="request">The enrolment and result details.</param>
        /// <returns>The newly recorded result.</returns>
        /// <response code="201">The result was created successfully.</response>
        /// <response code="400">The enrolment is not confirmed or the finishing position is invalid.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not authorised to record this result.</response>
        /// <response code="404">The enrolment does not exist.</response>
        /// <response code="409">A result already exists for this enrolment.</response>
        [HttpPost("results")]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateResult(
            [FromBody] CreateResultRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new
                {
                    message = "Please log in first."
                });

            if (role != "Organiser")
                return StatusCode(StatusCodes.Status403Forbidden);

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(
                    e => e.EnrolmentID == request.EnrolmentId);

            if (enrolment == null)
                return NotFound(new
                {
                    message = "Enrolment not found."
                });

            if (enrolment.Event == null ||
                enrolment.Event.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            if (enrolment.Status != "Confirmed")
            {
                return BadRequest(new
                {
                    message =
                        "Only confirmed enrolments can receive results."
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
                .AnyAsync(r =>
                    r.EnrolmentID == request.EnrolmentId);

            if (resultExists)
            {
                return Conflict(new
                {
                    message =
                        "A result already exists for this enrolment."
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

            return StatusCode(StatusCodes.Status201Created, ToDto(result));
        }

        /// <summary>
        /// Retrieves all results belonging to the logged-in participant.
        /// </summary>
        /// <returns>The participant's results.</returns>
        /// <response code="200">Results retrieved successfully.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not a participant.</response>
        [HttpGet("results/mine")]
        [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyResults()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized();

            if (role != "Participant")
                return StatusCode(StatusCodes.Status403Forbidden);

            var results = await _context.Results
                .Where(r =>
                    r.Enrolment != null &&
                    r.Enrolment.ParticipantID == userId.Value)
                .ToListAsync();

            return Ok(results.Select(ToDto));
        }

        /// <summary>
        /// Retrieves results for an event owned by the logged-in organiser.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <returns>The event's results.</returns>
        /// <response code="200">Results retrieved successfully.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not authorised to view these results.</response>
        /// <response code="404">The event does not exist.</response>
        [HttpGet("events/{eventId:int}/results")]
        [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventResults(int eventId)
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
                return NotFound(new
                {
                    message = "Event not found."
                });

            if (ev.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            var results = await _context.Results
                .Where(r =>
                    r.Enrolment != null &&
                    r.Enrolment.EventID == eventId)
                .ToListAsync();

            return Ok(results.Select(ToDto));
        }

        /// <summary>
        /// Updates the result for an event owned by the logged-in organiser.
        /// </summary>
        /// <param name="id">The result identifier.</param>
        /// <param name="request">The updated finishing time and position.</param>
        /// <returns>A confirmation that the result was updated.</returns>
        /// <response code="200">The result was updated successfully.</response>
        /// <response code="400">The finishing position is invalid.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not authorised to update this result.</response>
        /// <response code="404">The result or its associated enrolment or event does not exist.</response>
        [HttpPut("results/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateResult(
            int id,
            [FromBody] UpdateResultRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized();

            if (role != "Organiser")
                return StatusCode(StatusCodes.Status403Forbidden);

            var result = await _context.Results
                .Include(r => r.Enrolment)
                .ThenInclude(e => e.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
                return NotFound(new
                {
                    message = "Result not found."
                });

            if (result.Enrolment == null ||
                result.Enrolment.Event == null)
            {
                return NotFound(new
                {
                    message =
                        "Associated enrolment or event not found."
                });
            }

            if (result.Enrolment.Event.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            if (request.FinishingPosition.HasValue &&
                request.FinishingPosition.Value < 1)
            {
                return BadRequest(new
                {
                    message =
                        "Finishing position must be greater than zero."
                });
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
