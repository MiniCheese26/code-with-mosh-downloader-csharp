using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GenericHelperLibs;

namespace codeWithMoshDownloader.PageParser.Models
{
    internal class Lecture
    {
        public WistiaLectureStream? WistiaLectureStream { get; set; }
        public List<DirectLectureDownloadableAttachment> DownloadableLectureContent { get; } = new List<DirectLectureDownloadableAttachment>();
        public List<LectureHtmlFile> LectureHtmlFiles { get; } = new List<LectureHtmlFile>();
    }

    internal abstract class LectureAttachmentBase
    {
        private string _filename = "";

        public string Filename
        {
            get => _filename;
            set
            {
                value = string.Join("_", value.Split(Path.GetInvalidFileNameChars()));
                _filename = value;
            }
        }
    }

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

    internal class WistiaLectureStream : LectureDownloadableAttachmentBase
    {
        public WistiaLectureStream(CodeWithMoshClient codeWithMoshClient, Logger logger) : base(codeWithMoshClient, logger)
        {
        }

        public async Task<byte[]> Download(string qualityToDownload)
        {
            return new byte[4];
        }
    }

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

    internal class LectureHtmlFile : LectureAttachmentBase
    {
        public string Contents { get; set; } = "";

        public bool WriteHtmlFile(string downloadDirectory, Logger logger)
        {
            var filePath = Path.Join(downloadDirectory, Filename);

            try
            {
                using var htmlFile = File.CreateText(filePath);
                htmlFile.Write(Contents);
                htmlFile.Close();
            }
            catch (Exception ex)
            {
                logger.Log($"Failed to create or write to {Filename}, {ex.Message}");
                return false;
            }

            return true;
        }
    }
}