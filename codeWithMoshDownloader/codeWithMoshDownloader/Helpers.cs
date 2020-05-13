using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace codeWithMoshDownloader
{
    internal static class Helpers
    {
        public static string? GetAssemblyDirectoryPath() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }

    internal static class DownloaderHelpers
    {
        
    }
}