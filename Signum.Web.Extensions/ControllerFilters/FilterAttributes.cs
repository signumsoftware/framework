using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Web.Mvc;
using System.Diagnostics;
using Signum.Utilities;
using System.Web.Routing;
using Signum.Web;
using Signum.Entities.Authorization;
using Signum.Web.Authorization;
using Signum.Engine.Authorization;
using System.Threading;
using System.IO.Compression;
using System.IO;
using System.Text.RegularExpressions;

namespace Signum.Web
{
    public class TrackTimeFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var action = filterContext.RouteData.Values["controller"] + "." + filterContext.RouteData.Values["action"];

            filterContext.Controller.ViewData["elapsed"] = TimeTracker.Start(action);

            IDisposable profiler = HeavyProfiler.Log(aditionalData: action);
            if (profiler != null)
                filterContext.Controller.ViewData["profiler"] = profiler;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            IDisposable elapsed = (IDisposable)filterContext.Controller.ViewData.TryGetC("elapsed");
            if (elapsed != null)
                elapsed.Dispose();

            IDisposable profiler = (IDisposable)filterContext.Controller.ViewData.TryGetC("profiler");
            if (profiler != null)
                profiler.Dispose();
        }
    }

    public class GzipFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpRequestBase request = filterContext.HttpContext.Request;
            HttpResponseBase response = filterContext.HttpContext.Response;

            if (response.IsRequestBeingRedirected)
                return; //avoid send headers once headers have already been sent

            string acceptEncoding = request.Headers["Accept-Encoding"];
            if (string.IsNullOrEmpty(acceptEncoding)) return;
            acceptEncoding = acceptEncoding.ToUpperInvariant();

            if (response.Filter == null) return;

            if (response.Filter.GetType() == typeof(GZipStream) || response.Filter.GetType() == typeof(DeflateStream)) return;

            if (acceptEncoding.Contains("GZIP") )
            {
                response.AppendHeader("Content-encoding", "gzip");
                response.Filter = new GZipStream(response.Filter, CompressionMode.Compress);
            }
            else if (acceptEncoding.Contains("DEFLATE"))
            {
                response.AppendHeader("Content-encoding", "deflate");
                response.Filter = new DeflateStream(response.Filter, CompressionMode.Compress);
            }
        }
    }
       
    public class StripWhitespacesFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpRequestBase request = filterContext.HttpContext.Request;
            string acceptEncoding = request.Headers["Accept-Encoding"];
            if (string.IsNullOrEmpty(acceptEncoding)) return;
            acceptEncoding = acceptEncoding.ToUpperInvariant();
            HttpResponseBase response = filterContext.HttpContext.Response;
            response.Filter = new WhitespaceFilter(response.Filter);                                      
        }
    }

    public class WhitespaceFilter : Stream
    {

        public WhitespaceFilter(Stream sink)
        {
            _sink = sink;
        }

        private Stream _sink;
        private static Regex reg = new Regex(@"(?<=[^])\t{2,}|(?<=[>])\s{2,}(?=[<])|(?<=[>])\s{2,11}(?=[<])|(?=[\n])\s{2,}");

        #region Properites

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            _sink.Flush();
        }

        public override long Length
        {
            get { return 0; }
        }

        private long _position;
        public override long Position
        {
            get { return _position; }
            set { _position = value; }
        }

        #endregion

        #region Methods

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _sink.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _sink.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _sink.SetLength(value);
        }

        public override void Close()
        {
            _sink.Close();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte[] data = new byte[count];
            Buffer.BlockCopy(buffer, offset, data, 0, count);
            string html = System.Text.Encoding.Default.GetString(buffer);

            html = reg.Replace(html, string.Empty);

            byte[] outdata = System.Text.Encoding.Default.GetBytes(html);
            _sink.Write(outdata, 0, outdata.GetLength(0));
        }

        #endregion

    }
}
