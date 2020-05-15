using System;
using System.IO;
using GenericHelperLibs;

namespace codeWithMoshDownloader.PageParser.Models.Lectures
{
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