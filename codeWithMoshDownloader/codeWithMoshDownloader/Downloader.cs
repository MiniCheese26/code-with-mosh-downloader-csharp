using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using codeWithMoshDownloader.ArgumentParsing;
using codeWithMoshDownloader.PageParser;
using codeWithMoshDownloader.PageParser.Models.Lectures;
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
            const string testUrl = "https://codewithmosh.com/courses/228831/lectures/3563950";
            var l = new CodeWithMoshPageParser(_codeWithMoshClient, _logger);
            Lecture? e = await l.ParseLecture(testUrl);
            
            if (e == null)
                return false;
            
            (string? courseTitle, string? sectionTitle) = await l.ParseSectionAndCourseTitleFromLecture(
                testUrl);

            if (courseTitle == null)
            {
                courseTitle = DownloaderHelpers.GetRandomOutputName();
                _logger.Log($"Failed to parse course title from lecture page, using {courseTitle} as a random name");
            }
            else if (sectionTitle == null)
            {
                sectionTitle = DownloaderHelpers.GetRandomOutputName();
                _logger.Log($"Failed to parse section title from lecture page, using {sectionTitle} as a random name");
            }
            else
            {
                courseTitle.CleanStringForFolderName();
                sectionTitle.CleanStringForFolderName();
            }

            if (e.WistiaLectureStream != null)
            {
                if (_downloadArguments.CheckQuality)
                {
                    await e.WistiaLectureStream.PrintQualities();
                    return true;
                }

                var q = await e.WistiaLectureStream.Download(string.IsNullOrWhiteSpace(_downloadArguments.Quality)
                    ? _config.DefaultQuality
                    : _downloadArguments.Quality);
            }

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