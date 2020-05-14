using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace codeWithMoshDownloader.ArgumentParsing
{
    [Verb("download-id-rips", HelpText = "Use an exported JSON file with Wistia stream ID's and attachment URL's to download content, does not required paid access")]
    internal class RipArguments : ArgumentBase
    {
        [Option(HelpText = "Exported JSON file to use for downloading")]
        public override string Input { get; set; } = null!;

        [Usage(ApplicationAlias = "CodeWithMoshDownloader.exe")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new[]
                {
                    new Example("Exported JSON file to use for downloading", UnParserSettings.WithGroupSwitchesOnly(),
                        new RipArguments
                        {
                            Input = "output.json"
                        })
                };
            }
        }
    }
}