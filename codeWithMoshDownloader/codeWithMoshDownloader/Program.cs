using System.Threading.Tasks;
using Newtonsoft.Json;

namespace codeWithMoshDownloader
{
    internal static class Program
    {
        private static async Task Main()
        {
            
        }
    }

    internal class Config
    {
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }
        
        [JsonProperty("defaultQuality")]
        public string DefaultQuality { get; set; }
    }
}