
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Provides endpoints for managing event categories.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        /// <summary>
        /// Initializes the categories controller.
        /// </summary>
        /// <param name="context">The RaceDay database context.</param>
        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Converts a category entity into a category DTO.
        /// </summary>
        /// <param name="category">The category to convert.</param>
        /// <returns>The category data transfer object.</returns>
        private CategoryDto ToDto(Category category)
        {
            return new CategoryDto
            {
                CategoryId = category.CategoryID,
                EventId = category.EventID,
                CategoryName = category.CategoryName,
                CategoryType = category.CategoryType,
                MinAge = category.MinAge,
                MaxAge = category.MaxAge,
                Distance = category.Distance
            };
        }

        /// <summary>
        /// Checks whether the supplied category details are valid.
        /// </summary>
        /// <param name="request">The category details to validate.</param>
        /// <returns>True if the category details are valid.</returns>
        private bool IsValidCategory(CreateCategoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CategoryName))
                return false;

            if (request.CategoryType != "Age" &&
                request.CategoryType != "Distance")
                return false;

            if (request.MinAge < 0 || request.MaxAge < 0 ||
                request.Distance <= 0)
                return false;

            if (request.CategoryType == "Age")
            {
                return request.MinAge.HasValue &&
                       request.MaxAge.HasValue &&
                       request.MinAge.Value <= request.MaxAge.Value;
            }

            return request.Distance.HasValue;
        }

        /// <summary>
        /// Retrieves all categories belonging to an event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <returns>The event's categories.</returns>
        /// <response code="200">Categories retrieved successfully.</response>
        /// <response code="404">The event does not exist.</response>
        [HttpGet("events/{eventId:int}/categories")]
        [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            bool eventExists = await _context.Events
                .AnyAsync(e => e.EventID == eventId);

            if (!eventExists)
                return NotFound(new { message = "Event not found." });

            var categories = await _context.Categories
                .Where(c => c.EventID == eventId)
                .ToListAsync();

            return Ok(categories.Select(ToDto));
        }

        /// <summary>
        /// Creates a category for an event owned by the logged-in organiser.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="request">The category details.</param>
        /// <returns>The newly created category.</returns>
        /// <response code="201">The category was created successfully.</response>
        /// <response code="400">The category details are invalid.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not an authorised organiser.</response>
        /// <response code="404">The event does not exist.</response>
        [HttpPost("events/{eventId:int}/categories")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateCategory(
            int eventId,
            [FromBody] CreateCategoryRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "Please log in first." });

            if (role != "Organiser")
                return StatusCode(StatusCodes.Status403Forbidden);

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            if (!IsValidCategory(request))
            {
                return BadRequest(new
                {
                    message = "Provide a valid category type and matching age or distance values."
                });
            }

            var category = new Category
            {
                EventID = eventId,
                CategoryName = request.CategoryName.Trim(),
                CategoryType = request.CategoryType,
                MinAge = request.CategoryType == "Age"
                    ? request.MinAge : null,
                MaxAge = request.CategoryType == "Age"
                    ? request.MaxAge : null,
                Distance = request.CategoryType == "Distance"
                    ? request.Distance : null
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, ToDto(category));
        }

        /// <summary>
        /// Updates a category belonging to the logged-in organiser's event.
        /// </summary>
        /// <param name="id">The category identifier.</param>
        /// <param name="request">The updated category details.</param>
        /// <returns>The updated category.</returns>
        /// <response code="200">The category was updated successfully.</response>
        /// <response code="400">The supplied category details are invalid.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not authorised to update this category.</response>
        /// <response code="404">The category does not exist.</response>
        [HttpPut("categories/{id:int}")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory(
            int id,
            [FromBody] UpdateCategoryRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "Please log in first." });

            if (role != "Organiser")
                return StatusCode(StatusCodes.Status403Forbidden);

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.Event == null ||
                category.Event.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            if (!IsValidCategory(request))
            {
                return BadRequest(new
                {
                    message = "Provide a valid category type and matching age or distance values."
                });
            }

            category.CategoryName = request.CategoryName.Trim();
            category.CategoryType = request.CategoryType;
            category.MinAge = request.CategoryType == "Age"
                ? request.MinAge : null;
            category.MaxAge = request.CategoryType == "Age"
                ? request.MaxAge : null;
            category.Distance = request.CategoryType == "Distance"
                ? request.Distance : null;

            await _context.SaveChangesAsync();

            return Ok(ToDto(category));
        }

        /// <summary>
        /// Deletes a category if no participants are enrolled in it.
        /// </summary>
        /// <param name="id">The category identifier.</param>
        /// <returns>A no-content response when deletion succeeds.</returns>
        /// <response code="204">The category was deleted successfully.</response>
        /// <response code="401">The user is not logged in.</response>
        /// <response code="403">The user is not authorised to delete this category.</response>
        /// <response code="404">The category does not exist.</response>
        /// <response code="409">The category has existing enrolments.</response>
        [HttpDelete("categories/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "Please log in first." });

            if (role != "Organiser")
                return StatusCode(StatusCodes.Status403Forbidden);

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.Event == null ||
                category.Event.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            bool hasEnrolments = await _context.Enrolments
                .AnyAsync(e => e.CategoryID == id);

            if (hasEnrolments)
            {
                return Conflict(new
                {
                    message = "This category cannot be deleted because participants are enrolled in it."
                });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
