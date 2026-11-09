
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Contracts;
using System.Net;

namespace RaceDay.Mvc.Controllers
{
    public class AuthController : Controller
    {
        private readonly RaceDayApiClient _apiClient;

        public AuthController(RaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var loginRequest = new LoginRequest
            {
                Email = model.Email,
                Password = model.Password
            };

            var response = await _apiClient.PostAsync(
                "api/auth/login", loginRequest);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                return View(model);
            }

            var cookie = response.Headers
                .GetValues("Set-Cookie")
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(cookie))
            {
                var cookieValue = cookie.Split(';')[0];

                HttpContext.Session.SetString(
                    "ApiSessionCookie", cookieValue);
            }

            var authResponse = await response.Content
                .ReadFromJsonAsync<AuthResponse>();

            if (authResponse != null)
            {
                HttpContext.Session.SetString(
                    "UserId", authResponse.UserId.ToString());

                HttpContext.Session.SetString(
                    "UserName", authResponse.FirstName);

                HttpContext.Session.SetString(
                    "UserRole", authResponse.Role);
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var registerRequest = new RegisterRequest
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                Email = model.Email,
                Password = model.Password,
                Role = model.Role
            };

            var response = await _apiClient.PostAsync(
                "api/auth/register", registerRequest);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Registration failed. Please check your details.");

                return View(model);
            }

            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await _apiClient.PostAsync<object>(
                "api/auth/logout", new { });

            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}
