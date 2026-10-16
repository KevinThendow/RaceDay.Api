
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Provides endpoints for viewing and updating the logged-in user's profile.
    /// </summary>
    [ApiController]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        /// <summary>
        /// Initializes the profile controller.
        /// </summary>
        /// <param name="context">The RaceDay database context.</param>
        public ProfileController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves the profile of the currently logged-in user.
        /// </summary>
        /// <returns>The user's profile details.</returns>
        /// <response code="200">The profile was retrieved successfully.</response>
        /// <response code="401">The user is not logged in or the account no longer exists.</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyProfile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Please log in first."
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();

                return Unauthorized(new
                {
                    message = "User account not found."
                });
            }

            return Ok(new ProfileDto
            {
                UserId = user.UserID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Role = user.Role,
                ProfileImageUrl = user.ProfileImageUrl
            });
        }

        /// <summary>
        /// Updates the profile of the currently logged-in user.
        /// </summary>
        /// <param name="request">The profile details to update.</param>
        /// <returns>The updated profile or an error response.</returns>
        /// <response code="200">The profile was updated successfully.</response>
        /// <response code="400">The first name or last name is missing.</response>
        /// <response code="401">The user is not logged in or the account no longer exists.</response>
        [HttpPut("me")]
        [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UpdateProfileRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Please log in first."
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();

                return Unauthorized(new
                {
                    message = "User account not found."
                });
            }

            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest(new
                {
                    message = "First name and last name are required."
                });
            }

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.PhoneNumber = request.PhoneNumber;
            user.DateOfBirth = request.DateOfBirth;
            user.ProfileImageUrl = request.ProfileImageUrl;

            await _context.SaveChangesAsync();

            return Ok(new ProfileDto
            {
                UserId = user.UserID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Role = user.Role,
                ProfileImageUrl = user.ProfileImageUrl
            });
        }
    }
}
