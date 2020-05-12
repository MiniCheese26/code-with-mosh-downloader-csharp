using System.IO;
using Newtonsoft.Json;

namespace codeWithMoshDownloader
{
    internal class Config
    {
        [JsonProperty("sessionId")]
        public string? SessionId { get; set; }
        
        [JsonProperty("defaultQuality")]
        public string? DefaultQuality { get; set; }
        
        private string? _downloadLocation;

        [JsonProperty("downloadLocation")]
        public string? DownloadLocation
        {
            get => _downloadLocation;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    value = Path.Join(Helpers.GetAssemblyDirectoryPath(), "Downloads");
                
                _downloadLocation = value;
            }
        }
    }
}