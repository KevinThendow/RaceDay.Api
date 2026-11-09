
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RaceDay.Mvc.Services
{
    public class RaceDayApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RaceDayApiClient(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        private void AddSessionCookie()
        {
            var sessionCookie = _httpContextAccessor
                .HttpContext?
                .Session
                .GetString("ApiSessionCookie");

            _httpClient.DefaultRequestHeaders.Remove("Cookie");

            if (!string.IsNullOrEmpty(sessionCookie))
            {
                _httpClient.DefaultRequestHeaders.Add(
                    "Cookie", sessionCookie);
            }
        }

        public async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            AddSessionCookie();
            return await _httpClient.GetAsync(endpoint);
        }

        public async Task<HttpResponseMessage> PostAsync<T>(
            string endpoint, T data)
        {
            AddSessionCookie();
            return await _httpClient.PostAsJsonAsync(endpoint, data);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(
            string endpoint, T data)
        {
            AddSessionCookie();
            return await _httpClient.PutAsJsonAsync(endpoint, data);
        }

        public async Task<HttpResponseMessage> DeleteAsync(
            string endpoint)
        {
            AddSessionCookie();
            return await _httpClient.DeleteAsync(endpoint);
        }

        // Upload files using multipart/form-data.
        public async Task<HttpResponseMessage> PostFileAsync(
            string endpoint, IFormFile file)
        {
            AddSessionCookie();

            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);

            if (!string.IsNullOrWhiteSpace(file.ContentType))
            {
                fileContent.Headers.ContentType =
                    MediaTypeHeaderValue.Parse(file.ContentType);
            }

            content.Add(
                fileContent,
                "file",
                file.FileName);

            return await _httpClient.PostAsync(endpoint, content);
        }
    }
}
