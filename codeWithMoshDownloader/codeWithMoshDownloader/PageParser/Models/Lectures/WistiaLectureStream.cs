using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GenericHelperLibs;
using Newtonsoft.Json;

namespace codeWithMoshDownloader.PageParser.Models.Lectures
{
    internal class WistiaLectureStream : LectureDownloadableAttachmentBase
    {
        public WistiaLectureStream(CodeWithMoshClient codeWithMoshClient, Logger logger) : base(codeWithMoshClient, logger)
        {
        }

        public async Task<byte[]?> Download(string qualityToDownload)
        {
            using HttpResponseMessage? f = await CodeWithMoshClient.MakeGet(Url);

            if (f == null || !f.IsSuccessStatusCode)
                return null;
            
            var r = await f.Content.ReadAsStringAsync();
            var m = JsonConvert.DeserializeObject<WistiaMedia>(r);

            if (m?.Media?.Assets == null)
                return null;

            Asset? p = m.Media.Assets.FirstOrDefault(x => string.Equals(x.Resolution, qualityToDownload)) ??
                       m.Media.Assets.FirstOrDefault(x => string.Equals(x.Type, "original"));

            return new byte[4];
        }

        public async Task PrintQualities()
        {
            
        }
    }
    
    internal class WistiaMedia
    {
        [JsonProperty("media")]
        public Media? Media { get; set; }
    }

    internal class Media
    {
        [JsonProperty("assets")]
        public Asset[]? Assets { get; set; }
        
        [JsonProperty("name")]
        public string? Name { get; set; }
    }

    internal class Asset
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("url")]
        public Uri? Url { get; set; }
        
        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("height")]
        public long Height { get; set; }

        public string Resolution => $"{Width}x{Height}";
        
        [JsonProperty("bitrate")]
        public long Bitrate { get; set; }
        
        [JsonProperty("container", NullValueHandling = NullValueHandling.Ignore)]
        public string? Container { get; set; }
        
        [JsonProperty("opt_vbitrate", NullValueHandling = NullValueHandling.Ignore)]
        public long? OptVbitrate { get; set; }
    }
}