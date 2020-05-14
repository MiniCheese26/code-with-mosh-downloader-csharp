using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using codeWithMoshDownloader.ArgumentParsing;
using codeWithMoshDownloader.PageParser.Models;
using GenericHelperLibs;

namespace codeWithMoshDownloader
{
    internal class Downloader
    {
        private readonly Logger _logger;
        private readonly DownloadArguments _downloadArguments;
        private readonly Config _config;
        private readonly CodeWithMoshClient _codeWithMoshClient;

        public Downloader(Logger logger, DownloadArguments downloadArguments, Config config)
        {
            _logger = logger;
            _downloadArguments = downloadArguments;
            _config = config;
            _codeWithMoshClient = new CodeWithMoshClient(_config.SessionId ?? "", _logger);
        }
        
        public async Task Run()
        {
            var sessionCookieIsValid = await _codeWithMoshClient.CheckIfLoggedIn();
            
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

            if (!string.IsNullOrWhiteSpace(lectureId))
                await DownloadLecture();
            else
                await DownloadCourse();
        }

        private async Task<bool> DownloadLecture()
        {
            var l = new CodeWithMoshPageParser(_codeWithMoshClient, _logger);
            Lecture? e = await l.ParseLecture("https://codewithmosh.com/courses/223623/lectures/3517438");
            
            return true;
        }

        private async Task DownloadCourse()
        {
            
        }

        private (string courseId, string lectureId) ParseInputUrl()
        {
            Match urlMatch = Regex.Match(_downloadArguments.Input,
                @"https:\/\/codewithmosh\.com\/courses\/(?:enrolled\/)?(?'courseId'\d+)(?:\/lectures\/(?'lectureId'\d+))?",
                RegexOptions.IgnoreCase);

            return (urlMatch.Groups["courseId"].Value, urlMatch.Groups["lectureId"].Value);
        }
    }

    internal class CourseSection
    {
        public string? SectionTitle { get; set; }
        public List<string> SectionLectures { get; } = new List<string>();
    }
}