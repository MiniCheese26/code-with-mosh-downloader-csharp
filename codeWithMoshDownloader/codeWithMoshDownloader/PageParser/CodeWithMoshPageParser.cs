using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using codeWithMoshDownloader.PageParser;
using codeWithMoshDownloader.PageParser.Models;
using GenericHelperLibs;
using HtmlAgilityPack;

namespace codeWithMoshDownloader
{
    internal class CodeWithMoshPageParser
    {
        private readonly CodeWithMoshClient _codeWithMoshClient;
        private readonly Logger _logger;

        public CodeWithMoshPageParser(CodeWithMoshClient codeWithMoshClient, Logger logger)
        {
            _codeWithMoshClient = codeWithMoshClient;
            _logger = logger;
        }

        public async Task<Lecture?> ParseLecture(string lectureUrl)
        {
            HttpResponseMessage? lecturePageResponse = await _codeWithMoshClient.MakeGet(lectureUrl);

            if (lecturePageResponse == null)
            {
                _logger.Log($"Failed to get lecture page content of {lectureUrl}");
                return null;
            }

            var lecturePageContent = await lecturePageResponse.Content.ReadAsStringAsync();
            
            var lecturePageHtmlDocument = new HtmlDocument();
            lecturePageHtmlDocument.LoadHtml(lecturePageContent);

            var lectureHeadingNode = lecturePageHtmlDocument.DocumentNode.SelectSingleNode("//h2[@id='lecture_heading']");

            var lectureHeading = lectureHeadingNode.SafeGetHtmlNodeInnerText();

            var lectureWistiaPlayerNode =
                lecturePageHtmlDocument.DocumentNode.SelectSingleNode(
                    "//div[contains(@class, 'attachment-wistia-player')]");

            var lectureWistiaId =
                lectureWistiaPlayerNode.SafeAccessHtmlNode(x => x.Attributes["data-wistia-id"].Value);

            var lectureVideoEmbedDownloadNode =
                lecturePageHtmlDocument.DocumentNode.SelectSingleNode("//a[@class='download']");

            var lectureVideoEmbedDownloadUrl =
                lectureVideoEmbedDownloadNode.SafeAccessHtmlNode(x => x.Attributes["href"].Value);

            var lecture = new Lecture();

            if (string.IsNullOrWhiteSpace(lectureWistiaId) && !string.IsNullOrWhiteSpace(lectureVideoEmbedDownloadUrl))
            {
                _logger.Log($"No Wistia stream ID found for {lectureUrl}, falling back to embed link");
                
                var embedDirectDownload = new DirectLectureDownloadableAttachment(_codeWithMoshClient, _logger)
                {
                    Filename = lectureHeading + ".mp4",
                    Url = lectureVideoEmbedDownloadUrl
                };
                
                lecture.DownloadableLectureContent.Add(embedDirectDownload);
            }
            else if (string.IsNullOrWhiteSpace(lectureVideoEmbedDownloadUrl) && !string.IsNullOrWhiteSpace(lectureWistiaId))
            {
                _logger.Log($"Found Wistia ID for {lectureUrl}", true);
                
                var wistiaDownload = new WistiaLectureStream(_codeWithMoshClient, _logger)
                {
                    Filename = lectureHeading + ".mp4",
                    Url = lectureWistiaId
                };

                lecture.WistiaLectureStream = wistiaDownload;
            }
            else
            {
                _logger.Log($"No video found for {lectureUrl}", true);
            }

            var attachments = lecturePageHtmlDocument.DocumentNode.SelectNodes("//div[contains(@id, 'lecture-attachment') and not(contains(@class, 'lecture-attachment-type-video'))]");
            
            foreach (HtmlNode htmlNode in attachments)
            {
                var attachmentType = htmlNode.GetClasses().ToList()[1].Split("-").Last();

                switch (attachmentType)
                {
                    case "text":
                    case "html":
                        var messageHtml = htmlNode.SelectSingleNode("./div[@class='lecture-text-container']")
                            .SafeAccessHtmlNode(x => x.InnerHtml);
                        
                        if (messageHtml.Contains("May I ask you a favor?", StringComparison.OrdinalIgnoreCase))
                            continue;
                        
                        var l = new LectureHtmlFile
                        {
                            Contents = messageHtml,
                            Filename = lectureHeading + ".html"
                        };
                        break;
                    case "file":
                        
                        break;
                    case "pdf_embed":
                        
                        break;
                }
            }
            
            return lecture;
        }

        public async Task<(string? courseTitle, string? sectionTitle)> ParseSectionAndCourseTitleFromLecture(string lectureUrl)
        {
            HttpResponseMessage? lecturePageResponse = await _codeWithMoshClient.MakeGet(lectureUrl);

            if (lecturePageResponse == null)
            {
                _logger.Log($"Failed to get lecture page content of {lectureUrl}");
                return (null, null);
            }

            var lecturePageContent = await lecturePageResponse.Content.ReadAsStringAsync();
            
            var lecturePageHtmlDocument = new HtmlDocument();
            lecturePageHtmlDocument.LoadHtml(lecturePageContent);
            
            var lectureHeadingNode = lecturePageHtmlDocument.DocumentNode.SelectSingleNode("//h2[@id='lecture_heading']");

            var lectureHeading = lectureHeadingNode.SafeGetHtmlNodeInnerText();

            var sectionTitleNode = lecturePageHtmlDocument.DocumentNode.SelectSingleNode(
                $"//div[@class='section-title' and //span[contains(text(), '{lectureHeading}')]]");

            var sectionTitle = sectionTitleNode
                .SafeGetHtmlNodeInnerText()
                .CleanSectionTitleInnerText();

            var courseTitle = ParseCourseTitle(lecturePageHtmlDocument);
            
            return (courseTitle, sectionTitle);
        }

        public async Task<string?> ParseCourseTitle(string url)
        {
            HttpResponseMessage? pageResponse = await _codeWithMoshClient.MakeGet(url);

            if (pageResponse == null)
            {
                _logger.Log($"Failed to get page content of {url}");
                return null;
            }

            var pageContent = await pageResponse.Content.ReadAsStringAsync();
            
            var pageHtmlDocument = new HtmlDocument();
            pageHtmlDocument.LoadHtml(pageContent);

            return ParseCourseTitle(pageHtmlDocument);
        }
        
        private string ParseCourseTitle(HtmlDocument htmlDocument)
        {
            var courseTitleNode = htmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class, 'course-sidebar')]/h2");

            return courseTitleNode.SafeGetHtmlNodeInnerText();
        }

        public async IAsyncEnumerable<CourseSection> ParseCourseContents(string courseUrl)
        {
            HttpResponseMessage? coursePageResponse = await _codeWithMoshClient.MakeGet(courseUrl);

            if (coursePageResponse == null)
            {
                _logger.Log($"Failed to get course page content of {courseUrl}");
                yield break;
            }

            var coursePageContent = await coursePageResponse.Content.ReadAsStringAsync();
            
            var coursePageHtmlDocument = new HtmlDocument();
            coursePageHtmlDocument.LoadHtml(coursePageContent);

            var courseSectionNodes = coursePageHtmlDocument.DocumentNode.SelectNodes("//div[@class='row']/div");
            
            foreach (HtmlNode courseSectionNode in courseSectionNodes)
            {
                var sectionTitleNode = courseSectionNode.SelectSingleNode("./div[@class='section-title']");

                var sectionTitle = sectionTitleNode
                    .SafeGetHtmlNodeInnerText()
                    .CleanSectionTitleInnerText();

                var courseSection = new CourseSection
                {
                    SectionTitle = sectionTitle
                };

                var sectionLectureList = courseSectionNode.SelectSingleNode("./ul");
                
                if (sectionLectureList == null)
                    continue;

                courseSection.SectionLectures.AddRange(sectionLectureList.ChildNodes
                    .SelectMany(x => x.ChildNodes)
                    .Where(y => y.Name == "a")
                    .Select(z => "https://codewithmosh.com" + z.Attributes["href"].Value));

                yield return courseSection;
            }
        }
    }
}