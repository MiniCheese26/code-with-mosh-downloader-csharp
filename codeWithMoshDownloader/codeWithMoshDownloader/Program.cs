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
            var parser = new Parser(with => with.HelpWriter = null);

            var parserResult = parser.ParseArguments<Arguments>(args);

            parserResult
                .WithParsed(Run)
                .WithNotParsed(err => Fail(parserResult, err));
        }

        private static void Run(Arguments arguments)
        {
            var logger = new Logger(arguments.Debug);

            var config = Config.GetConfig(logger);
            
            if (config == null)
                Environment.Exit(1);
            
            var mainProcess = new MainProcess(logger, arguments, config);
            mainProcess.Run().GetAwaiter().GetResult();
        }

        private static void Fail(ParserResult<Arguments> result, IEnumerable<Error> errors)
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