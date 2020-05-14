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
using Newtonsoft.Json.Linq;

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

                // extract these later
                
                switch (attachmentType)
                {
                    case "text":
                    case "html":
                        var messageHtmlNode = htmlNode.SelectSingleNode("div[@class='lecture-text-container']")
                            .SafeAccessHtmlNode(x => x.InnerHtml);
                        
                        if (messageHtmlNode.Contains("May I ask you a favor?", StringComparison.OrdinalIgnoreCase))
                            continue;
                        
                        var htmlFile = new LectureHtmlFile
                        {
                            Contents = messageHtmlNode,
                            Filename = lectureHeading + ".html"
                        };
                        
                        lecture.LectureHtmlFiles.Add(htmlFile);
                        break;
                    case "quiz":
                        var answerJsonHtmlNode = htmlNode.FirstChild;
                        
                        if (answerJsonHtmlNode == null)
                            continue;

                        var answerJson =
                            HttpUtility.HtmlDecode(
                                answerJsonHtmlNode.SafeAccessHtmlNode(x => x.Attributes["data-data"].Value));
                        var questionJson =
                            HttpUtility.HtmlDecode(
                                answerJsonHtmlNode.SafeAccessHtmlNode(x => x.Attributes["data-schema"].Value));
                        
                        if (string.IsNullOrWhiteSpace(answerJson) || string.IsNullOrWhiteSpace(questionJson))
                            continue;

                        JObject l = PageParserHelpers.SafeJObjectParse(answerJson);
                        JObject g = PageParserHelpers.SafeJObjectParse(questionJson);

                        var p = g["properties"];
                        JEnumerable<JProperty>? r = l["answerKey"]?.Children<JProperty>();
                        
                        var t = new List<QuizItem>();
                        
                        break;
                    case "pdf_embed":
                        HtmlNode pdfHtmlNode = htmlNode.SelectSingleNode("div[@class='download-pdf']/div[last()]/a");

                        if (pdfHtmlNode == null)
                            continue;
                        
                        var pdfDownloadUrl = pdfHtmlNode.SafeAccessHtmlNode(x => x.Attributes["href"].Value);
                        var pdfFilename = pdfHtmlNode.SafeAccessHtmlNode(x => x.Attributes["data-x-origin-download-name"].Value);
                        
                        var pdfDownload = new DirectLectureDownloadableAttachment(_codeWithMoshClient, _logger)
                        {
                            Filename = pdfFilename,
                            Url = pdfDownloadUrl
                        };
                        
                        lecture.DownloadableLectureContent.Add(pdfDownload);
                        break;
                    case "file":
                    case "pdf":
                        HtmlNode fileHtmlNode = htmlNode.SelectSingleNode("div[last()]/a");
                        
                        if (fileHtmlNode == null)
                            continue;

                        var downloadUrl = fileHtmlNode.SafeAccessHtmlNode(x => x.Attributes["href"].Value);
                        var filename =
                            fileHtmlNode.SafeAccessHtmlNode(x => x.Attributes["data-x-origin-download-name"].Value);
                        
                        var fileDownload = new DirectLectureDownloadableAttachment(_codeWithMoshClient, _logger)
                        {
                            Filename = filename,
                            Url = downloadUrl
                        };
                        
                        lecture.DownloadableLectureContent.Add(fileDownload);
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
            
            HtmlNode lectureHeadingNode = lecturePageHtmlDocument.DocumentNode.SelectSingleNode("//h2[@id='lecture_heading']");

            var lectureHeading = lectureHeadingNode.SafeGetHtmlNodeInnerText();

            HtmlNode sectionTitleNode = lecturePageHtmlDocument.DocumentNode.SelectSingleNode(
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
            HtmlNode courseTitleNode = htmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class, 'course-sidebar')]/h2");

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

            HtmlNodeCollection courseSectionNodes = coursePageHtmlDocument.DocumentNode.SelectNodes("//div[@class='row']/div");
            
            foreach (HtmlNode courseSectionNode in courseSectionNodes)
            {
                HtmlNode sectionTitleNode = courseSectionNode.SelectSingleNode("div[@class='section-title']");

                var sectionTitle = sectionTitleNode
                    .SafeGetHtmlNodeInnerText()
                    .CleanSectionTitleInnerText();

                var courseSection = new CourseSection
                {
                    SectionTitle = sectionTitle
                };

                HtmlNode sectionLectureList = courseSectionNode.FirstChild.SelectSingleNode("ul");
                
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

    internal class QuizItem
    {
        public string? QuestionTitle { get; set; }
        public List<string> Questions { get; } = new List<string>();
        public List<string> Answers { get; } = new List<string>();
    }
}