using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GenericHelperLibs;

namespace codeWithMoshDownloader
{
    internal class CodeWithMoshClient
    {
        private const int RequestRetries = 3;

        private readonly HttpClient _httpClient;
        private readonly Logger _logger;

        public CodeWithMoshClient(string sessionId, Logger logger)
        {
            var sessionIdCookie = new Cookie("_session_id", sessionId, "/", "codewithmosh.com");
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(sessionIdCookie);
            var httpClientHandler = new HttpClientHandler {Proxy = null, UseProxy = false, CookieContainer = cookieContainer};
            _httpClient = new HttpClient(httpClientHandler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:76.0) Gecko/20100101 Firefox/76.0");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,en;q=0.5");

            _logger = logger;
        }

        public async Task<bool> CheckIfLoggedIn()
        {
            using HttpResponseMessage? mainPageResponse = await MakeGet("https://codewithmosh.com/");

            if (mainPageResponse == null || !mainPageResponse.IsSuccessStatusCode)
                return false;

            var mainPageContent = await mainPageResponse.Content.ReadAsStringAsync();

            return !mainPageContent.Contains("header-sign-up-btn", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<HttpResponseMessage?> MakeGet(string url)
        {
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            return await MakeRequest(httpRequestMessage);
        }
        
        public async Task<HttpResponseMessage?> MakePost(string url, HttpContent? httpContent = null)
        {
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);

            if (httpContent != null)
                httpRequestMessage.Content = httpContent;
            
            return await MakeRequest(httpRequestMessage);
        }
        
        public async Task<HttpResponseMessage?> MakeRequest(HttpRequestMessage httpRequestMessage)
        {
            for (int i = 0; i < RequestRetries; i++)
            {
                HttpResponseMessage response;

                try
                {
                    response = await _httpClient.SendAsync(httpRequestMessage);
                }
                catch (HttpRequestException ex)
                {
                    _logger.Log($"ATTEMPT {i + 1}: {httpRequestMessage.Method} request to {httpRequestMessage.RequestUri} failed, {ex.Message}");
                    continue;
                }

                if (response.IsSuccessStatusCode)
                    return response;
                
                _logger.Log($"ATTEMPT {i + 1}: {httpRequestMessage.Method} request to {httpRequestMessage.RequestUri} failed, {response.ReasonPhrase}");
            }

            _logger.Log($"{httpRequestMessage.Method} request to {httpRequestMessage.RequestUri} failed after {RequestRetries} attempts");
            return null;
        }
    }
}