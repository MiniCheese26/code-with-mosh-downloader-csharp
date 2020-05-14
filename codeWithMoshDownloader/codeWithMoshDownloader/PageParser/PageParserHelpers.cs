using System;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace codeWithMoshDownloader.PageParser
{
    internal static class PageParserHelpers
    {
        public static string SafeGetMatchValue(this Match match, string groupName) =>
            match.Groups.ContainsKey(groupName)
                ? match.Groups[groupName].Value
                : string.Empty;

        public static string SafeGetMatchValue(this Match match, int index) =>
            index <= match.Groups.Count - 1
                ? match.Groups[index].Value
                : string.Empty;

        public static string CleanSectionTitleInnerText(this string titleInnerText) =>
            Regex.Match(titleInnerText, @"(?'sectionTitle'[\w\W]+?)\(\d+m\)", RegexOptions.IgnoreCase)
                .SafeGetMatchValue("sectionTitle").Trim();

        public static string SafeGetHtmlNodeInnerText(this HtmlNode node) =>
            node == null
                ? string.Empty
                : HttpUtility.HtmlDecode(node.InnerText).Trim();

        public static string SafeAccessHtmlNode(this HtmlNode node, Func<HtmlNode, string> accessor)
        {
            if (node == null)
                return string.Empty;

            try
            {
                return accessor(node).Trim();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static JObject SafeJObjectParse(string stringToParse)
        {
            JObject jObject;

            try
            {
                jObject = JObject.Parse(stringToParse);
            }
            catch (JsonReaderException)
            {
                return new JObject();
            }

            return jObject;
        }
    }
}