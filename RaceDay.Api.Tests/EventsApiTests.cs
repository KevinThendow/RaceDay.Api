using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using Xunit;

namespace RaceDay.Api.Tests
{
    public class EventsApiTests
        : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public EventsApiTests(
            WebApplicationFactory<Program> factory)
        {
            var testFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<
                        DbContextOptions<RaceDayDbContext>>();

                    services.AddDbContext<RaceDayDbContext>(
                        options =>
                            options.UseInMemoryDatabase(
                                "RaceDayTestDatabase"));
                });
            });

            _client = testFactory.CreateClient();
        }

        [Fact]
        public async Task GetEvents_ReturnsSuccessStatusCode()
        {
            var response =
                await _client.GetAsync("/api/events");

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);
        }

        [Fact]
        public async Task GetProfile_WithoutLogin_ReturnsUnauthorized()
        {
            var response =
                await _client.GetAsync("/api/profile/me");

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        [Fact]


        public async Task CreateEvent_WithoutLogin_ReturnsUnauthorized()
        {
            var json = """
    {
        "eventName": "Test Race",
        "description": "Automated test event",
        "eventDate": "2026-11-15",
        "location": "Polokwane",
        "distance": 5.0,
        "eventType": "Run",
        "bannerImageUrl": null
    }
    """;

            var content = new StringContent(
                json,
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync(
                "/api/events",
                content);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }



        [Fact]
        public async Task Participant_CannotCreateEvent()
        {
            var email = $"participant_{Guid.NewGuid():N}@test.com";

            var registerData = new
            {
                firstName = "Test",
                lastName = "Participant",
                email = email,
                password = "TestPassword123!",
                role = "Participant",
                phoneNumber = "0712345678",
                dateOfBirth = "1997-05-08"
            };

            var registerContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(registerData),
                System.Text.Encoding.UTF8,
                "application/json");

            var registerResponse = await _client.PostAsync(
                "/api/auth/register",
                registerContent);

            Assert.True(
                registerResponse.IsSuccessStatusCode,
                await registerResponse.Content.ReadAsStringAsync());

            var loginData = new
            {
                email = email,
                password = "TestPassword123!"
            };

            var loginContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(loginData),
                System.Text.Encoding.UTF8,
                "application/json");

            var loginResponse = await _client.PostAsync(
                "/api/auth/login",
                loginContent);

            Assert.True(
                loginResponse.IsSuccessStatusCode,
                await loginResponse.Content.ReadAsStringAsync());

            var eventData = new
            {
                eventName = "Test Race",
                description = "Automated test event",
                eventDate = "2026-11-15",
                location = "Polokwane",
                distance = 5.0,
                eventType = "Run",
                bannerImageUrl = (string?)null
            };

            var eventContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(eventData),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync(
                "/api/events",
                eventContent);

            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);
        }





        [Fact]
        public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
        {
            var loginData = new
            {
                email = $"unknown_{Guid.NewGuid():N}@test.com",
                password = "WrongPassword123!"
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(loginData),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync(
                "/api/auth/login",
                content);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }




        [Fact]
        public async Task Login_WithWrongPassword_ReturnsUnauthorized()
        {
            var email = $"login_{Guid.NewGuid():N}@test.com";

            var registerData = new
            {
                firstName = "Test",
                lastName = "User",
                email = email,
                password = "CorrectPassword123!",
                role = "Participant",
                phoneNumber = "0712345678",
                dateOfBirth = "1997-05-08"
            };

            var registerContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(registerData),
                System.Text.Encoding.UTF8,
                "application/json");

            var registerResponse = await _client.PostAsync(
                "/api/auth/register",
                registerContent);

            Assert.True(
                registerResponse.IsSuccessStatusCode,
                await registerResponse.Content.ReadAsStringAsync());

            var loginData = new
            {
                email = email,
                password = "WrongPassword123!"
            };

            var loginContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(loginData),
                System.Text.Encoding.UTF8,
                "application/json");

            var loginResponse = await _client.PostAsync(
                "/api/auth/login",
                loginContent);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                loginResponse.StatusCode);
        }



        [Fact]
        public async Task Register_WithDuplicateEmail_ReturnsConflict()
        {
            var email = $"duplicate_{Guid.NewGuid():N}@test.com";

            var registerData = new
            {
                firstName = "Test",
                lastName = "User",
                email = email,
                password = "TestPassword123!",
                role = "Participant",
                phoneNumber = "0712345678",
                dateOfBirth = "1997-05-08"
            };

            var json =
                System.Text.Json.JsonSerializer.Serialize(registerData);

            var firstContent = new StringContent(
                json,
                System.Text.Encoding.UTF8,
                "application/json");

            var firstResponse = await _client.PostAsync(
                "/api/auth/register",
                firstContent);

            Assert.True(
                firstResponse.IsSuccessStatusCode,
                await firstResponse.Content.ReadAsStringAsync());

            var secondContent = new StringContent(
                json,
                System.Text.Encoding.UTF8,
                "application/json");

            var secondResponse = await _client.PostAsync(
                "/api/auth/register",
                secondContent);

            Assert.Equal(
                HttpStatusCode.Conflict,
                secondResponse.StatusCode);
        }



        [Fact]
        public async Task Register_WithInvalidRole_ReturnsBadRequest()
        {
            var registerData = new
            {
                firstName = "Test",
                lastName = "User",
                email = $"invalidrole_{Guid.NewGuid():N}@test.com",
                password = "TestPassword123!",
                role = "Administrator"
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(registerData),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync(
                "/api/auth/register",
                content);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }
    
[Fact]
        public async Task Register_WithMissingFields_ReturnsBadRequest()
        {
            var registerData = new
            {
                firstName = "",
                lastName = "",
                email = "",
                password = "",
                role = ""
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(registerData),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync(
                "/api/auth/register",
                content);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }
        [Fact]
        public async Task UpdateResult_WithoutLogin_ReturnsUnauthorized()
        {
            var json = """
    {
        "finishTime": "00:35:12",
        "finishingPosition": 1
    }
    """;

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _client.PutAsync(
                "/api/results/1",
                content);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }
    }
}