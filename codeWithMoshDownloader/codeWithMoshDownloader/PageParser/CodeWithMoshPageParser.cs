using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using codeWithMoshDownloader.PageParser.Models.Lectures;
using GenericHelperLibs;
using HtmlAgilityPack;

namespace codeWithMoshDownloader.PageParser
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
            using HttpResponseMessage? lecturePageResponse = await _codeWithMoshClient.MakeGet(lectureUrl);

            if (lecturePageResponse == null || !lecturePageResponse.IsSuccessStatusCode)
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
            else if (!string.IsNullOrWhiteSpace(lectureWistiaId))
            {
                _logger.Log($"Found Wistia ID for {lectureUrl}", true);
                
                var wistiaDownload = new WistiaLectureStream(_codeWithMoshClient, _logger)
                {
                    Filename = lectureHeading + ".mp4",
                    Url = $"https://fast.wistia.net/embed/medias/{lectureWistiaId}.json"
                };

                lecture.WistiaLectureStream = wistiaDownload;
            }
            else
            {
                _logger.Log($"No video found for {lectureUrl}", true);
            }

            var attachments = lecturePageHtmlDocument.DocumentNode.SelectNodes("//div[contains(@id, 'lecture-attachment') and not(contains(@class, 'lecture-attachment-type-video'))]");

            if (attachments == null)
                return lecture;
            
            foreach (HtmlNode htmlNode in attachments)
            {
                var attachmentType = htmlNode.GetClasses().ToList()[1].Split("-").Last();

                // extract these later
                
                switch (attachmentType)
                {
                    case "text":
                    case "html":
                        lecture.ProcessAndAddHtmlFile(htmlNode, lectureHeading);
                        break;
                    case "quiz":
                        break;
                        
                        // finish later, not important
                        
/*
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
*/
                    case "pdf_embed":
                        DirectLectureDownloadableAttachment? pdfEmbed = ProcessPdfEmbed(htmlNode);
                        
                        if (pdfEmbed != null)
                            lecture.DownloadableLectureContent.Add(pdfEmbed);
                        
                        break;
                    case "file":
                    case "pdf":
                        DirectLectureDownloadableAttachment? fileAttachment = ProcessFileAttachment(htmlNode);
                        
                        if (fileAttachment != null)
                            lecture.DownloadableLectureContent.Add(fileAttachment);
                        
                        break;
                }
            }
            
            return lecture;
        }

        private DirectLectureDownloadableAttachment? ProcessPdfEmbed(HtmlNode htmlNode)
        {
            HtmlNode pdfHtmlNode = htmlNode.SelectSingleNode("div[@class='download-pdf']/div[last()]/a");

            if (pdfHtmlNode == null)
                return null;
                        
            var pdfDownloadUrl = pdfHtmlNode.SafeAccessHtmlNode(x => x.Attributes["href"].Value);
            var pdfFilename = pdfHtmlNode.SafeAccessHtmlNode(x => x.Attributes["data-x-origin-download-name"].Value);
                        
            return new DirectLectureDownloadableAttachment(_codeWithMoshClient, _logger)
            {
                Filename = pdfFilename,
                Url = pdfDownloadUrl
            };
        }
        
        private DirectLectureDownloadableAttachment? ProcessFileAttachment(HtmlNode htmlNode)
        {
            HtmlNode fileHtmlNode = htmlNode.SelectSingleNode("div[last()]/a");
                        
            if (fileHtmlNode == null)
                return null;

            var downloadUrl = fileHtmlNode.SafeAccessHtmlNode(x => x.Attributes["href"].Value);
            var filename =
                fileHtmlNode.SafeAccessHtmlNode(x => x.Attributes["data-x-origin-download-name"].Value);
                        
            return new DirectLectureDownloadableAttachment(_codeWithMoshClient, _logger)
            {
                Filename = filename,
                Url = downloadUrl
            };
        }

        public async Task<(string? courseTitle, string? sectionTitle)> ParseSectionAndCourseTitleFromLecture(string lectureUrl)
        {
            using HttpResponseMessage? lecturePageResponse = await _codeWithMoshClient.MakeGet(lectureUrl);

            if (lecturePageResponse == null || !lecturePageResponse.IsSuccessStatusCode)
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
            using HttpResponseMessage? pageResponse = await _codeWithMoshClient.MakeGet(url);

            if (pageResponse == null || !pageResponse.IsSuccessStatusCode)
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
            using HttpResponseMessage? coursePageResponse = await _codeWithMoshClient.MakeGet(courseUrl);

            if (coursePageResponse == null || !coursePageResponse.IsSuccessStatusCode)
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