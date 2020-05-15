using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace codeWithMoshDownloader.PageParser.Models.Lectures
{
    internal class Lecture
    {
        public WistiaLectureStream? WistiaLectureStream { get; set; }
        public List<DirectLectureDownloadableAttachment> DownloadableLectureContent { get; } = new List<DirectLectureDownloadableAttachment>();
        public List<LectureHtmlFile> LectureHtmlFiles { get; } = new List<LectureHtmlFile>();

        public void ProcessAndAddHtmlFile(HtmlNode htmlNode, string lectureHeading)
        {
            var messageHtmlNode = htmlNode.SelectSingleNode("div[@class='lecture-text-container']")
                .SafeAccessHtmlNode(x => x.InnerHtml);
                        
            if (messageHtmlNode.Contains("May I ask you a favor?", StringComparison.OrdinalIgnoreCase))
                return;
                        
            var htmlFile = new LectureHtmlFile
            {
                Contents = messageHtmlNode,
                Filename = lectureHeading + ".html"
            };
                        
            LectureHtmlFiles.Add(htmlFile);
        }
    }
}