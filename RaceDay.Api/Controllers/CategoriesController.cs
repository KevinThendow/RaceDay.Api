
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

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

        // GET: /api/events/{eventId}/categories
        [HttpGet("events/{eventId:int}/categories")]
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

        // POST: /api/events/{eventId}/categories
        [HttpPost("events/{eventId:int}/categories")]
        public async Task<IActionResult> CreateCategory(
            int eventId,
            [FromBody] CreateCategoryRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "Please log in first." });

            if (role != "Organiser")
                return Forbid();

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganiserID != userId.Value)
                return Forbid();

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

            return StatusCode(201, ToDto(category));
        }

        // PUT: /api/categories/{id}
        [HttpPut("categories/{id:int}")]
        public async Task<IActionResult> UpdateCategory(
            int id,
            [FromBody] UpdateCategoryRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "Please log in first." });

            if (role != "Organiser")
                return Forbid();

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.Event == null ||
                category.Event.OrganiserID != userId.Value)
                return Forbid();

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

        // DELETE: /api/categories/{id}
        [HttpDelete("categories/{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return Unauthorized(new { message = "Please log in first." });

            if (role != "Organiser")
                return Forbid();

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.Event == null ||
                category.Event.OrganiserID != userId.Value)
                return Forbid();

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
