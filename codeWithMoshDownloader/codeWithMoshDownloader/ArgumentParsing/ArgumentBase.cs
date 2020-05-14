using CommandLine;

namespace codeWithMoshDownloader.ArgumentParsing
{
    internal abstract class ArgumentBase
    {
        [Option('d', "debug", HelpText = "Enables verbose output and logging to a log file")]
        public bool Debug { get; set; }
        
        [Option('i', "input", Required = true)]
        public virtual string Input { get; set; } = null!;
    }
}