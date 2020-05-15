using System.Threading.Tasks;
using GenericHelperLibs;

namespace codeWithMoshDownloader.PageParser.Models.Lectures
{
    internal class DirectLectureDownloadableAttachment : LectureDownloadableAttachmentBase
    {
        public DirectLectureDownloadableAttachment(CodeWithMoshClient codeWithMoshClient, Logger logger) : base(codeWithMoshClient, logger)
        {
        }
        
        public async Task<byte[]> Download()
        {
            return new byte[4];
        }
    }
}