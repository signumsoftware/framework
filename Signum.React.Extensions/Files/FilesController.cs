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

namespace Signum.React.Files
{
    public class FilesController : ApiController
    {
        [Route("api/files/downloadFile/{fileId}"), HttpGet]
        public HttpResponseMessage DownloadFile(string fileId)
        {
            var file = Database.Retrieve<FileEntity>(PrimaryKey.Parse(fileId, typeof(FileEntity)));

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var stream = new System.IO.MemoryStream(file.BinaryFile);
            response.Content = new StreamContent(stream);

            var mime = MimeMapping.GetMimeMapping(file.FileName);
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mime);

            return response;
        }

        [Route("api/files/downloadFilePath/{filePathId}"), HttpGet]
        public HttpResponseMessage DownloadFilePath(string filePathId)
        {
            var filePath = Database.Retrieve<FilePathEntity>(PrimaryKey.Parse(filePathId, typeof(FilePathEntity)));

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var stream = File.OpenRead(filePath.FullPhysicalPath);
            response.Content = new StreamContent(stream);

            var mime = MimeMapping.GetMimeMapping(filePath.FileName);
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mime);

            return response;
        }

        [Route("api/files/downloadEmbeddedFilePath/{fileType}"), HttpGet]
        public HttpResponseMessage DownloadEmbeddedFilePath(string fileTypeKey, string suffix, string fileName)
        {
            var fileType = SymbolLogic<FileTypeSymbol>.ToSymbol(fileTypeKey);

            var virtualFile = new EmbeddedFilePathEntity(fileType)
            {
                Suffix = suffix,
                FileName = fileName
            };

            var pair = FileTypeLogic.FileTypes.GetOrThrow(fileType).GetPrefixPair(virtualFile);

            var fullPhysicalPath = Path.Combine(pair.PhysicalPrefix, suffix);
            
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            var stream = File.OpenRead(fullPhysicalPath);
            response.Content = new StreamContent(stream);

            var mime = MimeMapping.GetMimeMapping(fullPhysicalPath);
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mime);

            return response;
        }
    }
}