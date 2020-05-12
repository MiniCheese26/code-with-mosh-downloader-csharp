using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace codeWithMoshDownloader.ArgumentParsing
{
    // Have to use null! as nullable's break CommandLineParser
    internal class Arguments
    {
        [Option('d', "debug", Required = false, HelpText = "Enables verbose output and logging to a log file")]
        public bool Debug { get; set; }
        
        [Option('f', "force-overwrite", Required = false, HelpText = "Force overwrite any existing files")]
        public bool ForceOverwrite { get; set; }
        
        [Option('q', "quality", Required = false, HelpText = "Select the quality used when downloading, overrides config quality | Example 1280x720")]
        public string Quality { get; set; } = null!;

        [Option('Q', "check-quality", Required = false, HelpText = "Print qualities available for requested content, will not download")]
        public bool CheckQuality { get; set; }

        private int _courseStartingIndex;
        
        [Option('s', "course-starting-index", Required = false, HelpText = "Set starting index for course download")]
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

        [Option('z', "auto-unzip-archives", Required = false, HelpText = "Auto extract downloaded zip archive contents")]
        public bool UnzipArchives { get; set; }
        
        [Option('i', "input-url", Required = true, HelpText = "Media to download")]
        public string InputUrl { get; set; } = null!;

        [Usage(ApplicationAlias = "CodeWithMoshDownloader")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new[]
                {
                    new Example("Pass url to download", UnParserSettings.WithGroupSwitchesOnly(),
                        new Arguments {
                            InputUrl = "https://codewithmosh.com/courses/enrolled/417695",
                            UnzipArchives = true,
                            ForceOverwrite = true,
                            Quality = "1280x720"
                        })
                };
            }
        }
    }
}