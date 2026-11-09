using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RaceDay.Mvc.Extensions;

namespace RaceDay.Mvc.Services
{
    public record ApiResult<T>(bool Success, int StatusCode, T? Data, string? Error);

    /// <summary>
    /// The only class that talks to the RaceDay API. It stores the API's session cookie in the
    /// MVC session on login and replays it on every call, so the API sees the same logged-in user.
    /// </summary>
    public class ApiClient
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _ctx;

        public ApiClient(HttpClient http, IHttpContextAccessor ctx)
        {
            _http = http;
            _ctx = ctx;
        }

        public Task<ApiResult<T>> GetAsync<T>(string url) =>
            SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, url));

        public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
            SendAsync<T>(WithJson(HttpMethod.Post, url, body));

        public Task<ApiResult<T>> PutAsync<T>(string url, object? body = null) =>
            SendAsync<T>(WithJson(HttpMethod.Put, url, body));

        public Task<ApiResult<T>> DeleteAsync<T>(string url) =>
            SendAsync<T>(new HttpRequestMessage(HttpMethod.Delete, url));

        /// <summary>Forwards an uploaded image to the API (form field name must be "file").</summary>
        public Task<ApiResult<T>> PostFileAsync<T>(string url, IFormFile file)
        {
            var content = new MultipartFormDataContent();
            var stream = new StreamContent(file.OpenReadStream());
            stream.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(stream, "file", file.FileName);
            return SendAsync<T>(new HttpRequestMessage(HttpMethod.Post, url) { Content = content });
        }

        private static HttpRequestMessage WithJson(HttpMethod method, string url, object? body)
        {
            var req = new HttpRequestMessage(method, url);
            if (body != null)
                req.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
            return req;
        }

        private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage req)
        {
            var session = _ctx.HttpContext!.Session;
            var cookie = session.GetString(UserSession.ApiCookieKey);
            if (!string.IsNullOrEmpty(cookie))
                req.Headers.TryAddWithoutValidation("Cookie", cookie);

            try
            {
                using var res = await _http.SendAsync(req);

                if (res.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    var apiCookie = cookies.FirstOrDefault(c => c.StartsWith(".RaceDay.Session="));
                    if (apiCookie != null)
                        session.SetString(UserSession.ApiCookieKey, apiCookie.Split(';')[0]);
                }

                var body = await res.Content.ReadAsStringAsync();

                if (res.IsSuccessStatusCode)
                {
                    T? data = default;
                    if (!string.IsNullOrWhiteSpace(body) && typeof(T) != typeof(object))
                        data = JsonSerializer.Deserialize<T>(body, Json);
                    return new ApiResult<T>(true, (int)res.StatusCode, data, null);
                }

                return new ApiResult<T>(false, (int)res.StatusCode, default, FriendlyError(res.StatusCode, body));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return new ApiResult<T>(false, 503, default, "The RaceDay service is unavailable right now. Please try again shortly.");
            }
            catch (JsonException)
            {
                return new ApiResult<T>(false, 502, default, "We received an unexpected response from the server.");
            }
        }

        // Never show raw errors to the user: prefer the API's { message }, otherwise a generic line.
        private static string FriendlyError(HttpStatusCode status, string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                    return m.GetString()!;
            }
            catch (JsonException) { /* fall through */ }

            return status switch
            {
                HttpStatusCode.BadRequest => "Some of the details you entered are not valid.",
                HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
                HttpStatusCode.Forbidden => "You are not allowed to do that.",
                HttpStatusCode.NotFound => "We could not find what you were looking for.",
                HttpStatusCode.Conflict => "That action conflicts with existing data.",
                _ => "Something went wrong. Please try again."
            };
        }
    }
}
