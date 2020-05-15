using System;
using System.IO;
using System.Reflection;

namespace codeWithMoshDownloader
{
    internal static class Helpers
    {
        public static string? GetAssemblyDirectoryPath() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }

    internal static class DownloaderHelpers
    {
        public static string GetRandomOutputName()
        {
            const string alphabet = "abcdefghijklmnopqrstuvwxyz";
            
            var random = new Random();

            var randomName = string.Empty;

            for (int i = 0; i < 10; i++)
            {
                randomName += alphabet[random.Next(0, alphabet.Length - 1)];
            }

            return randomName;
        }

        public static string CleanStringForFilename(this string input) =>
            string.Join("_", input.Split(Path.GetInvalidFileNameChars()));
        
        public static string CleanStringForFolderName(this string input) =>
            string.Join("_", input.Split(Path.GetInvalidPathChars()));
    }
}