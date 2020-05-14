using System;
using System.Collections.Generic;
using codeWithMoshDownloader.ArgumentParsing;
using CommandLine;
using CommandLine.Text;
using GenericHelperLibs;

namespace codeWithMoshDownloader
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            ParserResult<object> parserResult =
                Parser.Default.ParseArguments<DownloadArguments, RipInfoArguments, RipArguments>(args);

            parserResult
                .WithParsed<DownloadArguments>(Run)
                .WithParsed<RipInfoArguments>(Run)
                .WithParsed<RipArguments>(Run)
                .WithNotParsed(err => Fail(parserResult, err));
        }

        private static void Run(DownloadArguments downloadArguments)
        {
            (Config config, Logger logger) = GetConfigAndLogger(downloadArguments);
            
            var mainProcess = new Downloader(logger, downloadArguments, config);
            mainProcess.Run().GetAwaiter().GetResult();
        }

        private static void Run(RipInfoArguments ripInfoArguments)
        {
            (Config config, Logger logger) = GetConfigAndLogger(ripInfoArguments);
        }

        private static void Run(RipArguments ripArguments)
        {
            (Config config, Logger logger) = GetConfigAndLogger(ripArguments);
        }

        private static (Config config, Logger logger) GetConfigAndLogger(ArgumentBase argumentBase)
        {
            var logger = new Logger(argumentBase.Debug);
            var config = Config.GetConfig(logger);
            
            if (config == null)
                Environment.Exit(1);

            return (config, logger);
        }

        private static void Fail(ParserResult<object> result, IEnumerable<Error> errors)
        {
            HelpText? helpText;

            if (errors.IsVersion())
            {
                helpText = new HelpText($"{new HeadingInfo("CodeWithMoshDownloader", "1.0")}{Environment.NewLine}")
                    {
                        MaximumDisplayWidth = 200
                    }
                    .AddPreOptionsLine(Environment.NewLine);
            }
            else
            {
                helpText = HelpText.AutoBuild(result, h =>
                {
                    h.MaximumDisplayWidth = 200;
                    h.Heading = "CodeWithMoshDownloader 1.0";
                    return HelpText.DefaultParsingErrorsHandler(result, h);
                }, e => e);
            }

            Console.WriteLine(helpText);
            Environment.Exit(1);
        }
    }
}