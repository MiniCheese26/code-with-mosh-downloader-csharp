using System.IO;
using GenericHelperLibs;
using Newtonsoft.Json;

namespace codeWithMoshDownloader
{
    internal class Config
    {
        [JsonProperty("sessionId")]
        public string? SessionId { get; set; }

        private string _defaultQuality = "1920x1080";
        
        [JsonProperty("defaultQuality")]
        public string DefaultQuality
        {
            get => _defaultQuality;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    value = "1920x1080";
                
                _defaultQuality = value;
            }
        }

        private string _downloadLocation = Path.Join(Helpers.GetAssemblyDirectoryPath(), "Downloads");

        [JsonProperty("downloadLocation")]
        public string DownloadLocation
        {
            get => _downloadLocation;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    value = Path.Join(Helpers.GetAssemblyDirectoryPath(), "Downloads");
                
                _downloadLocation = value;
            }
        }

        public static Config? GetConfig(Logger logger)
        {
            string? assemblyPath = Helpers.GetAssemblyDirectoryPath();

            if (assemblyPath == null)
            {
                logger.Log("Failed to get assembly location, can't load config");
                return null;
            }

            var configPath = Path.Combine(assemblyPath, "config.json");

            var configLoader = new ConfigLoader<Config>(configPath);
            (Config? config, string failureMessage) = configLoader.LoadConfig();

            if (config != null)
                return config;
            
            logger.Log($"Failed to load config, can't load session id, {failureMessage}");
            return null;
        }
    }
}