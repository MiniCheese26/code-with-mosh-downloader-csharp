using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace codeWithMoshDownloader.ArgumentParsing
{
    [Verb("rip-ids", HelpText = "Capture and export Wistia stream ID's and attachment URL's of a course/lecture for people without paid access to use, requires paid access")]
    internal class RipInfoArguments : ArgumentBase
    {
        [Option(HelpText = "Pass url to content to parse, can be the courses page, a course or a lecture")]
        public override string Input { get; set; } = null!;

        [Usage(ApplicationAlias = "CodeWithMoshDownloader.exe")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new[]
                {
                    new Example("Pass url to content to parse, can be the courses page, a course or a lecture", UnParserSettings.WithGroupSwitchesOnly(),
                        new RipInfoArguments
                        {
                            Input = "https://codewithmosh.com/courses/"
                        })
                };
            }
        }
    }
}