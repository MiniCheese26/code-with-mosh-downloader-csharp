using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using codeWithMoshDownloader.ArgumentParsing;
using CommandLine;
using CommandLine.Text;
using GenericHelperLibs;

namespace codeWithMoshDownloader
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var parser = new Parser(with => with.HelpWriter = null);

            var parserResult = parser.ParseArguments<Arguments>(args);

            parserResult
                .WithParsed(Run)
                .WithNotParsed(err => Fail(parserResult, err));
        }

        private static void Run(Arguments arguments)
        {
            var logger = new Logger(arguments.Debug);
            var mainProcess = new MainProcess(logger, arguments);
            mainProcess.Run().GetAwaiter().GetResult();
        }

        private static void Fail(ParserResult<Arguments> result, IEnumerable<Error> errors)
        {
            HelpText? helpText;

            if (errors.IsVersion())
            {
                helpText = new HelpText($"{new HeadingInfo("CodeWithMoshDownloader", "1.0")}{Environment.NewLine}")
                    {
                        MaximumDisplayWidth = 200
                    }
                    .AddPreOptionsLine(Environment.NewLine);
            }
            else
            {
                helpText = HelpText.AutoBuild(result, h =>
                {
                    h.MaximumDisplayWidth = 200;
                    h.Heading = "CodeWithMoshDownloader 1.0";
                    return HelpText.DefaultParsingErrorsHandler(result, h);
                }, e => e);
            }

            Console.WriteLine(helpText);
            Environment.Exit(1);
        }
    }

    internal class MainProcess
    {
        private readonly Logger _logger;
        private readonly Arguments _arguments;

        public MainProcess(Logger logger, Arguments arguments)
        {
            _logger = logger;
            _arguments = arguments;
        }
        
        public async Task Run()
        {
            Config? config = LoadConfig();
            
            if (config == null)
                Environment.Exit(1);
            
            var codeWithMoshDownloader = new CodeWithMoshDownloader(config.SessionId ?? "", _logger);
            var sessionCookieIsValid = await codeWithMoshDownloader.ValidateSessionCookie();
            
            if (!sessionCookieIsValid)
            {
                _logger.Log("Failed to validate given session id cookie, cannot login");
                Environment.Exit(1);
            }

            (string courseId, string lectureId) = ParseInputUrl();

            if (string.IsNullOrWhiteSpace(courseId) && string.IsNullOrWhiteSpace(lectureId))
            {
                _logger.Log("Failed to parse both course and lecture ID from input url");
                Environment.Exit(1);
            }

            if (string.IsNullOrWhiteSpace(courseId) && !string.IsNullOrWhiteSpace(lectureId))
                await DownloadLecture();
            else
                await DownloadCourse();
        }

        private async Task DownloadLecture()
        {
            
        }

        private async Task DownloadCourse()
        {
            
        }

        private Config? LoadConfig()
        {
            string? assemblyPath = Helpers.GetAssemblyDirectoryPath();

            if (assemblyPath == null)
            {
                _logger.Log("Failed to get assembly location, can't load config");
                return null;
            }

            var configPath = Path.Combine(assemblyPath, "config.json");

            var configLoader = new ConfigLoader<Config>(configPath);
            (Config? config, string failureMessage) = configLoader.LoadConfig();

            if (config != null)
                return config;
            
            _logger.Log($"Failed to load config, can't load session id, {failureMessage}");
            return null;
        }

        private (string courseId, string lectureId) ParseInputUrl()
        {
            var urlMatch = Regex.Match(_arguments.InputUrl,
                @"https:\/\/codewithmosh\.com\/courses\/(?:enrolled\/)?(?'courseId'\d+)(?:\/lectures\/(?'lectureId'\d+))?",
                RegexOptions.IgnoreCase);

            return (urlMatch.Groups["courseId"].Value, urlMatch.Groups["lectureId"].Value);
        }
    }

    internal class CodeWithMoshDownloader
    {
        private const int RequestRetries = 3;
        
        private readonly HttpClient _httpClient;
        private readonly Logger _logger;

        public CodeWithMoshDownloader(string sessionId, Logger logger)
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

        public async Task<bool> ValidateSessionCookie()
        {
            HttpResponseMessage? mainPageResponse = await MakeGet("https://codewithmosh.com/");

            if (mainPageResponse == null)
                return false;

            var mainPageContent = await mainPageResponse.Content.ReadAsStringAsync();

            return !mainPageContent.Contains("header-sign-up-btn", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<HttpResponseMessage?> MakeGet(string url)
        {
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            return await MakeRequest(httpRequestMessage);
        }
        
        private async Task<HttpResponseMessage?> MakePost(string url, HttpContent? httpContent = null)
        {
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);

            if (httpContent != null)
                httpRequestMessage.Content = httpContent;
            
            return await MakeRequest(httpRequestMessage);
        }
        
        private async Task<HttpResponseMessage?> MakeRequest(HttpRequestMessage httpRequestMessage)
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

    internal static class Helpers
    {
        public static string? GetAssemblyDirectoryPath() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }
}