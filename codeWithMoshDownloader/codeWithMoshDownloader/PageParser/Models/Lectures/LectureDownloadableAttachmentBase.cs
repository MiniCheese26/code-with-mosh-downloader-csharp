using GenericHelperLibs;

namespace codeWithMoshDownloader.PageParser.Models.Lectures
{
    internal abstract class LectureDownloadableAttachmentBase : LectureAttachmentBase
    {
        protected readonly CodeWithMoshClient CodeWithMoshClient;
        protected readonly Logger Logger;
        
        public string Url { get; set; } = "";

        protected LectureDownloadableAttachmentBase(CodeWithMoshClient codeWithMoshClient, Logger logger)
        {
            CodeWithMoshClient = codeWithMoshClient;
            Logger = logger;
        }
    }
}