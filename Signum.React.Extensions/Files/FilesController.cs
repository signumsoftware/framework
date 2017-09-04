using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
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

namespace Signum.React.Files
{
    public class FilesController : ApiController
    {
        [Route("api/files/downloadFile/{fileId}"), HttpGet]
        public HttpResponseMessage DownloadFile(string fileId)
        {
            var file = Database.Retrieve<FileEntity>(PrimaryKey.Parse(fileId, typeof(FileEntity)));
            
            return GetHttpReponseMessage(new System.IO.MemoryStream(file.BinaryFile), file.FileName);

        }

        [Route("api/files/downloadFilePath/{filePathId}"), HttpGet]
        public HttpResponseMessage DownloadFilePath(string filePathId)
        {
            var filePath = Database.Retrieve<FilePathEntity>(PrimaryKey.Parse(filePathId, typeof(FilePathEntity)));

            return GetHttpReponseMessage(filePath.OpenRead(), filePath.FileName);
        }

        [Route("api/files/downloadEmbeddedFilePath/{fileTypeKey}"), HttpGet]
        public HttpResponseMessage DownloadFilePathEmbedded(string fileTypeKey, string suffix, string fileName)
        {
            var fileType = SymbolLogic<FileTypeSymbol>.ToSymbol(fileTypeKey);

            var virtualFile = new FilePathEmbedded(fileType)
            {
                Suffix = suffix,
                FileName = fileName,
            };
            
            return GetHttpReponseMessage(virtualFile.OpenRead(), virtualFile.FileName);
        }

        
        /// <param name="stream">No need to close</param
        public static HttpResponseMessage GetHttpReponseMessage(Stream stream, string fileName, bool forDownload = true)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            if (forDownload)
            {
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = Path.GetFileName(fileName)
                };
            }
            var mime = MimeMapping.GetMimeMapping(fileName);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mime);
            return response;
        }
    }
}