using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace codeWithMoshDownloader.ArgumentParsing
{
    // Have to use null! as nullable's break CommandLineParser
    [Verb("download", true, HelpText = "Download a course/lecture from codewithmosh.com, requires paid access")]
    internal class DownloadArguments : ArgumentBase
    {
        [Option('f', "force-overwrite", SetName = "normal-download", HelpText = "Force overwrite any existing files")]
        public bool ForceOverwrite { get; set; }
        
        [Option('q', "quality", SetName = "normal-download", HelpText = "Select the quality used when downloading, overrides config quality | Example 1280x720")]
        public string Quality { get; set; } = null!;

        [Option('Q', "check-quality", SetName = "normal-download", HelpText = "Print qualities available for requested content, will not download")]
        public bool CheckQuality { get; set; }

        private int _courseStartingIndex;
        
        [Option('s', "course-starting-index", SetName = "normal-download", HelpText = "Set starting index for course download")]
        public int CourseStartingIndex
        {
            get => _courseStartingIndex;
            set
            {
                if (value <= 0)
                    value = 1;

                _courseStartingIndex = value;
            }
        }

        [Option('z', "auto-unzip-archives", SetName = "normal-download", HelpText = "Auto extract downloaded zip archive contents")]
        public bool UnzipArchives { get; set; }

        [Option(Required = true, HelpText = "Url of course/lecture to download")]
        public override string Input { get; set; } = null!;

        [Usage(ApplicationAlias = "CodeWithMoshDownloader.exe")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new[]
                {
                    new Example("Pass url to download", UnParserSettings.WithGroupSwitchesOnly(),
                        new DownloadArguments {
                            UnzipArchives = true,
                            ForceOverwrite = true,
                            Quality = "1280x720"
                        })
                };
            }
        }
    }
}