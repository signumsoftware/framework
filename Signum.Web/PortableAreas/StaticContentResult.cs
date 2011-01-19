using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Text;
using System.IO.Compression;
using Signum.Utilities;

namespace Signum.Web.PortableAreas
{
    public class StaticContentResult : ActionResult
    {
        public string ContentType { get; set; }
        public TimeSpan CacheDuration = TimeSpan.FromDays(10);
        public byte[] Compressed { get; private set; }
        public byte[] Uncompressed { get; private set; }
        public bool IsText;

        public StaticContentResult(byte[] uncompressedContent, string fileName)
        {
            this.ContentType = MimeType.FromFileName(fileName);
            this.Uncompressed = uncompressedContent;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            HttpResponseBase response = context.HttpContext.Response;
            HttpRequestBase request = context.HttpContext.Request;

            response.AppendHeader("Vary", "Accept-Encoding");
            response.Cache.SetCacheability(HttpCacheability.Public);
            //response.Cache.SetExpires(DateTime.Now.Add(CacheDuration));   //redundant
            response.Cache.SetMaxAge(CacheDuration);

            if (!VersionChanged(request, ScriptHtmlHelper.Manager.LastModified))
            {
                response.StatusCode = 304;
                response.SuppressContent = true;
                return;
            }

            bool canGZip = CanGZip(context.HttpContext.Request);

            byte[] bytes = canGZip ? (Compressed ?? (Compressed = Compress())) : Uncompressed;

            response.AppendHeader("Content-Length", bytes.Length.ToString());
            if (canGZip)
                response.AppendHeader("Content-Encoding", "gzip");
            else if (IsText)
            {
                response.ContentEncoding = Encoding.Unicode;
            }

            response.Cache.SetLastModified(ScriptHtmlHelper.Manager.LastModified);

            response.ContentType = ContentType;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Flush();
        }

        private bool VersionChanged(HttpRequestBase request, DateTime contentModified)
        {
            string header = request.Headers["If-Modified-Since"];

            if (header != null)
            {
                DateTime isModifiedSince;
                if (DateTime.TryParse(header, out isModifiedSince))
                {
                    return contentModified > isModifiedSince;
                }
            }

            return true;
        }

        private byte[] Compress()
        {
            using (MemoryStream memoryStream = new MemoryStream(8092))
            {
                using (GZipStream stream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    using (MemoryStream uncompressedStream = new MemoryStream(Uncompressed))
                    {
                        uncompressedStream.CopyTo(stream);
                    }
                }

                return memoryStream.ToArray();
            }
        }

        private bool CanGZip(HttpRequestBase request)
        {
            string acceptEncoding = request.Headers["Accept-Encoding"];
            if (!string.IsNullOrEmpty(acceptEncoding) &&
                 (acceptEncoding.Contains("gzip") || acceptEncoding.Contains("deflate")))
                return true;
            return false;
        }
    }
}