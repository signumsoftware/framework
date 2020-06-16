using Microsoft.AspNetCore.Mvc;
using Signum.Entities;
using Signum.Entities.Files;
using Signum.Engine;
using Signum.Engine.Files;
using System.IO;
using Signum.Engine.Mailing;
using Signum.Entities.Basics;

namespace Signum.React.Files
{
    public class FilesController : ControllerBase
    {
        [HttpGet("api/files/downloadFile/{fileId}")]
        public FileStreamResult DownloadFile(string fileId)
        {
            var file = Database.Retrieve<FileEntity>(PrimaryKey.Parse(fileId, typeof(FileEntity)));

            return GetFileStreamResult(new MemoryStream(file.BinaryFile), file.FileName);

        }

        [HttpGet("api/files/downloadFilePath/{filePathId}")]
        public FileStreamResult DownloadFilePath(string filePathId)
        {
            var filePath = Database.Retrieve<FilePathEntity>(PrimaryKey.Parse(filePathId, typeof(FilePathEntity)));

            return GetFileStreamResult(filePath.OpenRead(), filePath.FileName);
        }

        [HttpGet("api/files/downloadEmbeddedFilePath/{fileTypeKey}")]
        public FileStreamResult DownloadFilePathEmbedded(string fileTypeKey, string suffix, string fileName)
        {
            var fileType = SymbolLogic<FileTypeSymbol>.ToSymbol(fileTypeKey);

            var virtualFile = new FilePathEmbedded(fileType)
            {
                Suffix = suffix,
                FileName = fileName,
            };

            return GetFileStreamResult(virtualFile.OpenRead(), virtualFile.FileName);
        }

        /// <param name="stream">No need to close</param
        public static FileStreamResult GetFileStreamResult(Stream stream, string fileName, bool forDownload = true)
        {
            var mime = MimeMapping.GetMimeType(fileName);
            return new FileStreamResult(stream, mime)
            {
                FileDownloadName = forDownload ? Path.GetFileName(fileName) : null
            };
        }

        public static FileStreamResult GetFileStreamResult(FileContent file, bool forDownload = true)
        {
            return GetFileStreamResult(new MemoryStream(file.Bytes), file.FileName);
        }
    }
}
