
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly RaceDayApiClient _apiClient;

        public ParticipantController(RaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // Display upcoming events with filtering and search.
        public async Task<IActionResult> Index(string? type, string? search)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Auth");

            if (role != "Participant")
                return RedirectToAction("Index", "Home");

            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            var response = await _apiClient.GetAsync("api/events");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Unable to load upcoming events.";
                return View(new List<EventDto>());
            }

            var events = await response.Content
                .ReadFromJsonAsync<List<EventDto>>()
                ?? new List<EventDto>();

            events = events
                .Where(e => e.EventDate.Date >= DateTime.Today)
                .ToList();

            if (!string.IsNullOrWhiteSpace(type))
            {
                events = events
                    .Where(e => e.EventType != null &&
                        e.EventType.Equals(
                            type,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                events = events
                    .Where(e =>
                        (e.EventName != null &&
                         e.EventName.Contains(
                             search,
                             StringComparison.OrdinalIgnoreCase))
                        ||
                        (e.Location != null &&
                         e.Location.Contains(
                             search,
                             StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            ViewBag.SelectedType = type;
            ViewBag.Search = search;

            return View(events);
        }

        // Display event details and available categories.
        public async Task<IActionResult> Details(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Auth");

            if (role != "Participant")
                return RedirectToAction("Index", "Home");

            var eventResponse = await _apiClient.GetAsync(
                $"api/events/{id}");

            if (!eventResponse.IsSuccessStatusCode)
                return NotFound();

            var raceEvent = await eventResponse.Content
                .ReadFromJsonAsync<EventDto>();

            if (raceEvent == null)
                return NotFound();

            var categoryResponse = await _apiClient.GetAsync(
                $"api/events/{id}/categories");

            if (!categoryResponse.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage =
                    "Unable to load event categories.";

                ViewBag.Categories = new List<CategoryDto>();
            }
            else
            {
                var categories = await categoryResponse.Content
                    .ReadFromJsonAsync<List<CategoryDto>>();

                ViewBag.Categories =
                    categories ?? new List<CategoryDto>();
            }

            return View(raceEvent);
        }

        // Submit a participant's event entry.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnterEvent(
            int eventId, int categoryId)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Auth");

            if (role != "Participant")
                return RedirectToAction("Index", "Home");

            var request = new CreateEnrolmentRequest
            {
                EventId = eventId,
                CategoryId = categoryId
            };

            var response = await _apiClient.PostAsync(
                "api/enrolments", request);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] =
                    "Unable to enter the event. Please try again.";

                return RedirectToAction(
                    "Details", new { id = eventId });
            }

            TempData["Success"] =
                "Your event entry has been submitted successfully!";

            return RedirectToAction(
                "Details", new { id = eventId });
        }

        // Display the participant's event entries.
        public async Task<IActionResult> MyEvents()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Auth");

            if (role != "Participant")
                return RedirectToAction("Index", "Home");

            var response = await _apiClient.GetAsync(
                "api/enrolments/mine");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage =
                    "Unable to load your event entries.";

                return View(new List<EnrolmentDto>());
            }

            var enrolments = await response.Content
                .ReadFromJsonAsync<List<EnrolmentDto>>()
                ?? new List<EnrolmentDto>();

            return View(enrolments);
        }

        // Display the participant's results.
        public async Task<IActionResult> MyResults()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Auth");

            if (role != "Participant")
                return RedirectToAction("Index", "Home");

            var response = await _apiClient.GetAsync(
                "api/results/mine");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage =
                    "Unable to load your results.";

                return View(new List<ResultDto>());
            }

            var results = await response.Content
                .ReadFromJsonAsync<List<ResultDto>>()
                ?? new List<ResultDto>();

            return View(results);
        }

        // Display the participant's profile.
        public async Task<IActionResult> Profile()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Auth");

            if (role != "Participant")
                return RedirectToAction("Index", "Home");

            var response = await _apiClient.GetAsync("api/profile/me");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage =
                    "Unable to load your profile.";

                return View(new ProfileDto());
            }

            var profile = await response.Content
                .ReadFromJsonAsync<ProfileDto>();

            return View(profile ?? new ProfileDto());
        }

        // Update the participant's profile details.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            UpdateProfileRequest request)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Auth");

            if (role != "Participant")
                return RedirectToAction("Index", "Home");

            var response = await _apiClient.PutAsync(
                "api/profile/me", request);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] =
                    "Unable to update your profile. Please try again.";
            }
            else
            {
                TempData["Success"] =
                    "Your profile was updated successfully.";
            }

            return RedirectToAction(nameof(Profile));
        }

        // Upload the participant's profile picture.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfileImage(
            IFormFile file)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Auth");

            if (role != "Participant")
                return RedirectToAction("Index", "Home");

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select an image.";
                return RedirectToAction(nameof(Profile));
            }

            var response = await _apiClient.PostFileAsync(
                "api/profile/upload-image", file);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetails =
                    await response.Content.ReadAsStringAsync();

                TempData["Error"] =
                    $"Upload failed ({(int)response.StatusCode}): {errorDetails}";
            }
            else
            {
                TempData["Success"] =
                    "Your profile picture was uploaded successfully.";
            }

            return RedirectToAction(nameof(Profile));
        }
    }
}
