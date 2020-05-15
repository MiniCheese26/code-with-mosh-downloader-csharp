using System.IO;

namespace codeWithMoshDownloader.PageParser.Models.Lectures
{
    internal abstract class LectureAttachmentBase
    {
        private string _filename = "";

        public string Filename
        {
            get => _filename;
            set => _filename = value.CleanStringForFilename();
        }
    }
}