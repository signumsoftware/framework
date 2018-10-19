using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.Entities.Omnibox;
using Signum.Entities.Files;
using Signum.Engine;
using System.Web;
using Signum.Engine.Files;
using System.IO;
using System.Net.Http.Headers;
using Signum.React.ApiControllers;
using Microsoft.AspNetCore.StaticFiles;
using Signum.Engine.Mailing;

namespace Signum.React.Files
{
    public class FilesController : ApiController
    {
        [HttpGet("api/files/downloadFile/{fileId}")]
        public FileStreamResult DownloadFile(string fileId)
        {
            var file = Database.Retrieve<FileEntity>(PrimaryKey.Parse(fileId, typeof(FileEntity)));
            
            return GetFileStreamResult(new System.IO.MemoryStream(file.BinaryFile), file.FileName);

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
    }
}