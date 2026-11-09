
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
using RaceDay.Contracts;
using System.Net;

namespace RaceDay.Mvc.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly RaceDayApiClient _apiClient;

        public OrganiserController(RaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (role != "Organiser")
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            return View();
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent(
            CreateEventRequest model)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _apiClient.PostAsync(
                "api/events", model);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                TempData["SuccessMessage"] =
                    "Race event created successfully!";

                return RedirectToAction("Index");
            }

            var errorMessage = await response.Content.ReadAsStringAsync();

            ViewBag.ErrorMessage =
                $"Unable to create event. API response: {errorMessage}";

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Events()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            var response = await _apiClient.GetAsync("api/events/mine");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Unable to load your events.";
                return View(new List<EventDto>());
            }

            var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();

            return View(events);
        }


        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            var response = await _apiClient.GetAsync($"api/events/{id}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Unable to load event details.";
                return RedirectToAction("Events");
            }

            var raceEvent = await response.Content
                .ReadFromJsonAsync<EventDto>();

            return View(raceEvent);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(
            int id, EventDto model)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _apiClient.PutAsync(
                $"api/events/{id}", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] =
                    "Event updated successfully!";

                return RedirectToAction("Events");
            }

            ViewBag.ErrorMessage = "Unable to update event.";
            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            var response = await _apiClient.DeleteAsync(
                $"api/events/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] =
                    "Event deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Unable to delete event. Please try again.";
            }

            return RedirectToAction("Events");
        }


        [HttpGet]
        public async Task<IActionResult> Categories(int eventId)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            // Confirm that this event belongs to the logged-in organiser.
            var myEventsResponse = await _apiClient.GetAsync("api/events/mine");

            if (!myEventsResponse.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Unable to verify event ownership.";
                return RedirectToAction("Events");
            }

            var myEvents = await myEventsResponse.Content
                .ReadFromJsonAsync<List<EventDto>>();

            if (myEvents == null || !myEvents.Any(e => e.EventId == eventId))
            {
                TempData["ErrorMessage"] = "You cannot manage categories for this event.";
                return RedirectToAction("Events");
            }

            var response = await _apiClient.GetAsync(
                $"api/events/{eventId}/categories");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Unable to load event categories.";
                ViewBag.EventId = eventId;
                return View(new List<CategoryDto>());
            }

            var categories = await response.Content
                .ReadFromJsonAsync<List<CategoryDto>>();

            ViewBag.EventId = eventId;

            return View(categories ?? new List<CategoryDto>());
        }
        [HttpGet]
        public async Task<IActionResult> CreateCategory(int eventId)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
                return RedirectToAction("Login", "Auth");

            var response = await _apiClient.GetAsync("api/events/mine");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("Events");

            var myEvents = await response.Content
                .ReadFromJsonAsync<List<EventDto>>();

            if (myEvents == null || !myEvents.Any(e => e.EventId == eventId))
            {
                TempData["ErrorMessage"] = "You cannot manage this event.";
                return RedirectToAction("Events");
            }

            ViewBag.EventId = eventId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(
            int eventId, CreateCategoryRequest model)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                ViewBag.EventId = eventId;
                return View(model);
            }

            var response = await _apiClient.PostAsync(
                $"api/events/{eventId}/categories", model);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                TempData["SuccessMessage"] = "Category created successfully!";
                return RedirectToAction("Categories", new { eventId });
            }

            ViewBag.EventId = eventId;
            ViewBag.ErrorMessage = "Unable to create category. Please check your details.";

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id, int eventId)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
                return RedirectToAction("Login", "Auth");

            var eventsResponse = await _apiClient.GetAsync("api/events/mine");

            if (!eventsResponse.IsSuccessStatusCode)
                return RedirectToAction("Events");

            var myEvents = await eventsResponse.Content
                .ReadFromJsonAsync<List<EventDto>>();

            if (myEvents == null || !myEvents.Any(e => e.EventId == eventId))
            {
                TempData["ErrorMessage"] = "You cannot manage this event.";
                return RedirectToAction("Events");
            }

            var response = await _apiClient.GetAsync(
                $"api/events/{eventId}/categories");

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Unable to load categories.";
                return RedirectToAction("Categories", new { eventId });
            }

            var categories = await response.Content
                .ReadFromJsonAsync<List<CategoryDto>>();

            var category = categories?.FirstOrDefault(c => c.CategoryId == id);

            if (category == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction("Categories", new { eventId });
            }

            var model = new UpdateCategoryRequest
            {
                CategoryName = category.CategoryName,
                CategoryType = category.CategoryType,
                MinAge = category.MinAge,
                MaxAge = category.MaxAge,
                Distance = category.Distance
            };

            ViewBag.CategoryId = id;
            ViewBag.EventId = eventId;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(
            int id, int eventId, UpdateCategoryRequest model)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                ViewBag.CategoryId = id;
                ViewBag.EventId = eventId;
                return View(model);
            }

            var response = await _apiClient.PutAsync(
                $"api/categories/{id}", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Category updated successfully!";
                return RedirectToAction("Categories", new { eventId });
            }

            ViewBag.CategoryId = id;
            ViewBag.EventId = eventId;
            ViewBag.ErrorMessage = "Unable to update category. Please check your details.";

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id, int eventId)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
                return RedirectToAction("Login", "Auth");

            var response = await _apiClient.DeleteAsync(
                $"api/categories/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Category deleted successfully!";
            }
            else if (response.StatusCode == HttpStatusCode.Conflict)
            {
                TempData["ErrorMessage"] =
                    "This category cannot be deleted because it has existing enrolments.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Unable to delete category. Please try again.";
            }

            return RedirectToAction("Categories", new { eventId });
        }



        [HttpGet]
        public async Task<IActionResult> Enrolments(int eventId)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
                return RedirectToAction("Login", "Auth");

            // Verify that the event belongs to this organiser.
            var eventsResponse = await _apiClient.GetAsync("api/events/mine");

            if (!eventsResponse.IsSuccessStatusCode)
                return RedirectToAction("Events");

            var myEvents = await eventsResponse.Content
                .ReadFromJsonAsync<List<EventDto>>();

            if (myEvents == null || !myEvents.Any(e => e.EventId == eventId))
            {
                TempData["ErrorMessage"] =
                    "You cannot view enrolments for this event.";

                return RedirectToAction("Events");
            }

            // Retrieve enrolments through the API.
            var response = await _apiClient.GetAsync(
                $"api/events/{eventId}/enrolments");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Unable to load event enrolments.";
                ViewBag.EventId = eventId;

                return View(new List<EnrolmentDto>());
            }

            var enrolments = await response.Content
                .ReadFromJsonAsync<List<EnrolmentDto>>();

            ViewBag.EventId = eventId;

            return View(enrolments ?? new List<EnrolmentDto>());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEnrolmentStatus(
        int id, int eventId, string status)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
                return RedirectToAction("Login", "Auth");

            var request = new UpdateEnrolmentStatusRequest
            {
                Status = status
            };

            var response = await _apiClient.PutAsync(
                $"api/enrolments/{id}/status", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] =
                    "Enrolment status updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Unable to update enrolment status.";
            }

            return RedirectToAction("Enrolments", new { eventId });
        }


        [HttpGet]
        public async Task<IActionResult> Results(int eventId)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
                return RedirectToAction("Login", "Auth");

            // Verify event ownership.
            var eventsResponse = await _apiClient.GetAsync("api/events/mine");

            if (!eventsResponse.IsSuccessStatusCode)
                return RedirectToAction("Events");

            var myEvents = await eventsResponse.Content
                .ReadFromJsonAsync<List<EventDto>>();

            if (myEvents == null ||
                !myEvents.Any(e => e.EventId == eventId))
            {
                TempData["ErrorMessage"] =
                    "You cannot view results for this event.";

                return RedirectToAction("Events");
            }

            // Retrieve event enrolments.
            var enrolmentsResponse = await _apiClient.GetAsync(
                $"api/events/{eventId}/enrolments");

            var enrolments = new List<EnrolmentDto>();

            if (enrolmentsResponse.IsSuccessStatusCode)
            {
                enrolments = await enrolmentsResponse.Content
                    .ReadFromJsonAsync<List<EnrolmentDto>>()
                    ?? new List<EnrolmentDto>();
            }

            // Only confirmed enrolments can receive results.
            ViewBag.ConfirmedEnrolments = enrolments
                .Where(e => e.Status == "Confirmed")
                .ToList();

            // Retrieve existing results.
            var resultsResponse = await _apiClient.GetAsync(
                $"api/events/{eventId}/results");

            ViewBag.EventId = eventId;

            if (!resultsResponse.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Unable to load event results.";
                return View(new List<ResultDto>());
            }

            var results = await resultsResponse.Content
                .ReadFromJsonAsync<List<ResultDto>>();

            return View(results ?? new List<ResultDto>());
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResult(
            int eventId, CreateResultRequest request)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Organiser")
                return RedirectToAction("Login", "Auth");

            var response = await _apiClient.PostAsync(
                "api/results", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] =
                    "Race result recorded successfully.";
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();

                TempData["ErrorMessage"] =
                    "Unable to record result. " + errorMessage;
            }

            return RedirectToAction("Results", new { eventId });
        }

    }
}


