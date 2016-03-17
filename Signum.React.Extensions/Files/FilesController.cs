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

namespace Signum.React.Files
{
    public class FilesController : ApiController
    {
        [Route("api/files/downloadFile"), HttpGet]
        public HttpResponseMessage DownloadFile(Lite<FileEntity> file)
        {
            var entity = file.Retrieve();

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var stream = new System.IO.MemoryStream(entity.BinaryFile);
            response.Content = new StreamContent(stream);

            var mime = MimeMapping.GetMimeMapping(entity.FileName);
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mime);

            return response;
        }

        [Route("api/files/downloadFilePath"), HttpGet]
        public HttpResponseMessage DownloadFilePath(Lite<FilePathEntity> file)
        {
            var entity = file.Retrieve();

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var stream = new System.IO.MemoryStream(entity.BinaryFile);
            response.Content = new StreamContent(stream);

            var mime = MimeMapping.GetMimeMapping(entity.FileName);
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mime);

            return response;
        }
    }
}