using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Web.Configuration;
using System.Security;
using System.Configuration;
using System.Security.Permissions;
using System.Net;
using Signum.Utilities;

namespace Signum.Web.ScriptCombiner
{
    public class MixedCssScriptCombiner
    {
        private readonly static TimeSpan CACHE_DURATION = TimeSpan.FromDays(30); //avoid 304 during a session

        /// <summary>
        /// Indicates if the output file should be cached
        /// </summary>
        protected bool cacheable = true;
        protected bool gzipable = true;

        static string Extension = "css";
        static string contentType = "text/css";

        static string lastModifiedDateKey = "-lmd";

        HttpContextBase context;

        public MixedCssScriptCombiner(HttpContextBase context)
        {
            this.context = context;
        }

        public static string Minify(string content)
        {
            content = Regex.Replace(content, @"/\*.*?\*/", "");
            content = Regex.Replace(content, "(\\s{2,}|\\t+|\\r+|\\n+)", string.Empty);
            content = content.Replace(" {", "{");
            content = content.Replace("{ ", "{");
            content = content.Replace(" :", ":");
            content = content.Replace(": ", ":");
            content = content.Replace(", ", ",");
            content = content.Replace("; ", ";");
            content = content.Replace(";}", "}");
            content = Regex.Replace(content, "/\\*[^\\*]*\\*+([^/\\*]*\\*+)*/", "$1");
            content = Regex.Replace(content, "(?<=[>])\\s{2,}(?=[<])|(?<=[>])\\s{2,}(?=&nbsp;)|(?<=&ndsp;)\\s{2,}(?=[<])", string.Empty);

            content = Regex.Replace(content, "[^\\}]+\\{\\}", string.Empty);  //Eliminamos reglas vacías

            return content;
        }

        private string GetCacheKey(string setName)
        {
            return "HttpCombiner." + setName;
        }

        private string GetCacheKey()
        {
            return "HttpCombiner." + GetUniqueKey();
        }

        public string GetUniqueKey()
        {
            return context.Request.Url.PathAndQuery.ToUpperInvariant().GetHashCode().ToString("x", System.Globalization.CultureInfo.InvariantCulture);
        }


        private bool WriteFromCache(bool isCompressed, DateTime lastModificationDate)
        {
            byte[] responseBytes = context.Cache[GetCacheKey()] as byte[];

            if (responseBytes == null || responseBytes.Length == 0)
                return false;

            //Compare with the date of the server cache content
            DateTime lmd = (DateTime)context.Cache[GetCacheKey() + lastModifiedDateKey];

            if (lmd != lastModificationDate) return false;

            this.WriteBytes(responseBytes, isCompressed);
            return true;
        }

        private void WriteBytes(byte[] bytes, bool isCompressed)
        {
            HttpResponseBase response = context.Response;

            response.AppendHeader("Content-Length", bytes.Length.ToString());
            response.ContentType = contentType;

            if (isCompressed)
                response.AppendHeader("Content-Encoding", "gzip");
            else
                response.AppendHeader("Content-Encoding", "utf-8");

            //response.Cache.SetVaryByCustom("Accept-Encoding");
            response.AppendHeader("Vary", "Accept-Encoding");
            response.Cache.SetCacheability(HttpCacheability.Public);
            response.Cache.SetExpires(DateTime.Now.Add(CACHE_DURATION));
            response.Cache.SetMaxAge(CACHE_DURATION);
            //response.Cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
            response.ContentEncoding = Encoding.Unicode;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Flush();
        }




        private bool CanGZip(HttpRequestBase request)
        {
            string acceptEncoding = request.Headers["Accept-Encoding"];
            if (!string.IsNullOrEmpty(acceptEncoding) &&
                 (acceptEncoding.Contains("gzip") || acceptEncoding.Contains("deflate")))
                return true;
            return false;
        }

        public void Process(List<IScriptResource> resources)
        {

            DateTime lmServer = DateTime.MinValue;
            foreach (var resource in resources)
            {
                DateTime fileLastModified = resource.GetLastModifiedDate();
                if (fileLastModified > lmServer) lmServer = fileLastModified;
            }

            //check dates
            if (context.Request["HTTP_IF_MODIFIED_SINCE"] != null)
            {
                DateTime lmBrowser = DateTime.Parse(context.Request["HTTP_IF_MODIFIED_SINCE"].ToString()).ToUniversalTime();

                if (lmServer.Date == lmBrowser.Date && Math.Truncate(lmServer.TimeOfDay.TotalSeconds) <= lmBrowser.TimeOfDay.TotalSeconds)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = (int)HttpStatusCode.NotModified;
                    context.Response.SuppressContent = true;
                    context.Response.Cache.SetCacheability(HttpCacheability.Public);
                    context.Response.Cache.SetExpires(DateTime.Now.Add(CACHE_DURATION));    //F5 will make the browser re-check the files
                    context.Response.Cache.SetMaxAge(CACHE_DURATION);
                    //context.Response.Cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
                    return;
                }
            }

            context.Response.Cache.SetLastModified(lmServer);

            // Decide if browser supports compressed response
            bool isCompressed = this.CanGZip(context.Request);

            // If the set has already been cached, write the response directly from
            // cache. Otherwise generate the response and cache it
            if (cacheable && !this.WriteFromCache(isCompressed, lmServer) || !cacheable)
            {
                using (MemoryStream memoryStream = new MemoryStream(8092))
                {
                    // Decide regular stream or gzip stream based on whether the response can be compressed or not
                    //using (Stream writer = isCompressed ?  (Stream)(new GZipStream(memoryStream, CompressionMode.Compress)) : memoryStream)
                    using (Stream writer = isCompressed ?
                                (Stream)(new GZipStream(memoryStream, CompressionMode.Compress)) :
                                memoryStream)
                    {

                        // Read the files into one big string
                        StringBuilder allScripts = new StringBuilder();
                        foreach (var resource in resources)
                        {
                            if (resource.fileName.EndsWith(Extension.StartsWith(".") ? Extension : "." + Extension))
                            {
                                try
                                {
                                    allScripts.Append(resource.ReadFile(context));
                                }
                                catch (Exception) { }
                            }
                        }
                        string minified = allScripts.ToString();
                        minified = Minify(minified);

                        // Send minfied string to output stream
                        byte[] bts = Encoding.UTF8.GetBytes(minified);
                        writer.Write(bts, 0, bts.Length);
                    }

                    // Cache the combined response so that it can be directly written
                    // in subsequent calls 
                    byte[] responseBytes = memoryStream.ToArray();
                    if (cacheable)
                    {
                        context.Cache.Insert(GetCacheKey(),
                        responseBytes, null, System.Web.Caching.Cache.NoAbsoluteExpiration,
                        CACHE_DURATION);

                        context.Cache.Insert(GetCacheKey() + lastModifiedDateKey,
                        lmServer, null, System.Web.Caching.Cache.NoAbsoluteExpiration,
                        CACHE_DURATION);
                    }

                    // Generate the response
                    this.WriteBytes(responseBytes, isCompressed);

                }
            }
        }
    }

    public class CssScriptResource : IScriptResource
    {
        public CssScriptResource(string fileName)
        {
            this.fileName = fileName;
            this.resourcesFolder = "../content";
        }

        public override string ReadFile(HttpContextBase context)
        {
            string file = context.Server.MapPath(Path.Combine(resourcesFolder, fileName));

            return Common.ReplaceRelativeImg(File.ReadAllText(file), fileName);
        }

    }

    public class AreaCssScriptResource : IScriptResource
    {
        public AreaCssScriptResource(string fileName)
        {
            this.fileName = fileName;
            this.resourcesFolder = "../Content";
        }

        public override DateTime GetLastModifiedDate()
        {
            AssemblyResourceStore ars = AssemblyResourceManager.GetResourceStoreFromVirtualPath("~/" + fileName);

            FileInfo fileInfo = new FileInfo(ars.Assembly.Location);
            return fileInfo.LastWriteTimeUtc;
        }

        public override string ReadFile(HttpContextBase context)
        {
            AssemblyResourceStore ars = AssemblyResourceManager.GetResourceStoreFromVirtualPath("~/" + fileName);
            Stream stream = ars.GetResourceStream("~/" + fileName);
            byte[] bytes = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(bytes, 0, (int)stream.Length);
            string content = Encoding.UTF8.GetString(bytes);

            return Common.ReplaceRelativeImg(content, fileName);
        }

    }

    public class JsScriptCombiner : ScriptCombiner
    {
        public JsScriptCombiner()
        {
            this.contentType = "application/x-javascript";
            this.cacheable = true;
            this.gzipable = true;
            this.resourcesFolder = "../Scripts";
        }
        protected override string Extension { get { return "js"; } }
        protected override string Minify(string content)
        {
            var minifier = new JavaScriptMinifier();
            string minified = minifier.Minify(content);
            string closure = GoogleClosure.CompressSourceCode(minified);
            if (closure.HasText())
                return closure;
            return minified;
        }
    }

    public class AreaJsScriptCombiner : JsScriptCombiner
    {
        public AreaJsScriptCombiner()
        {
            this.cacheable = true;
            this.gzipable = true;
            this.resourcesFolder = null;
        }

        public override DateTime GetLastModifiedDate(string fileName)
        {
            AssemblyResourceStore ars = AssemblyResourceManager.GetResourceStoreFromVirtualPath("~/" + fileName);

            FileInfo fileInfo = new FileInfo(ars.Assembly.Location);
            return fileInfo.LastWriteTimeUtc;
        }

        public override string ReadFile(string fileName)
        {
            AssemblyResourceStore ars = AssemblyResourceManager.GetResourceStoreFromVirtualPath("~/" + fileName);
            Stream stream = ars.GetResourceStream("~/" + fileName);
            byte[] bytes = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(bytes, 0, (int)stream.Length);
            return Encoding.UTF8.GetString(bytes);
        }
    }

    public abstract class IScriptResource
    {
        /// <summary>
        /// Sets the Content-Type HTTP Header
        /// </summary>
        protected string contentType;

        /// <summary>
        /// The folder where the scripts are stored
        /// </summary>
        public string resourcesFolder;


        /// <summary>
        /// Indicates if the output stream should be gzipped (depending on client request too)
        /// </summary>
        protected bool gzipable;

        /// <summary>
        /// Current version (increase to get a fresh output file)
        /// </summary>
        protected string version;

        /// <summary>
        /// File extension in order to prevent files reading manipulating the url
        /// </summary>
        public string fileName;

        private readonly static TimeSpan CACHE_DURATION = TimeSpan.FromDays(30); //1 day to avoid 304 during a session
       

        public virtual string ReadFile(HttpContextBase context)
        {
            string file = context.Server.MapPath((!string.IsNullOrEmpty(resourcesFolder) ? (resourcesFolder + "/") : "") + fileName.Replace("%2f", "/"));
            return File.ReadAllText(file);
        }

        public virtual DateTime GetLastModifiedDate() {
            return File.GetLastWriteTimeUtc(HttpContext.Current.Server.MapPath(resourcesFolder) + "/" + fileName);        
        }
       



        /*  public static string GetScriptTags(string setName, int version)
          {
              string result = null;
  #if (DEBUG)
              foreach (string fileName in GetScriptFileNames(setName))
              {
                  result += String.Format("\n<script type=\"text/javascript\" src=\"{0}?v={1}\"></script>", VirtualPathUtility.ToAbsolute(fileName), version);
              }
  #else
          result += String.Format("<script type=\"text/javascript\" src=\"ScriptCombiner.axd?s={0}&v={1}\"></script>", setName, version);
  #endif
              return result;
          }*/



    }

    public abstract class ScriptCombiner
    {
        /// <summary>
        /// Sets the Content-Type HTTP Header
        /// </summary>
        protected string contentType;

        /// <summary>
        /// The folder where the scripts are stored
        /// </summary>
        public string resourcesFolder;

        /// <summary>
        /// Indicates if the output file should be cached
        /// </summary>
        protected bool cacheable;

        /// <summary>
        /// Indicates if the output stream should be gzipped (depending on client request too)
        /// </summary>
        protected bool gzipable;

        /// <summary>
        /// Current version (increase to get a fresh output file)
        /// </summary>
        protected string version;

        /// <summary>
        /// File extension in order to prevent files reading manipulating the url
        /// </summary>
        protected abstract string Extension { get; }

        protected abstract string Minify(string content);

        private readonly static TimeSpan CACHE_DURATION = TimeSpan.FromDays(30); //1 day to avoid 304 during a session
        internal HttpContextBase context;
        string lastModifiedDateKey = "-lmd";

        public virtual string ReadFile(string fileName)
        {
            string file = context.Server.MapPath((!string.IsNullOrEmpty(resourcesFolder) ? (resourcesFolder + "/") : "") + fileName.Replace("%2f", "/"));
            return File.ReadAllText(file);
        }

        public virtual DateTime GetLastModifiedDate(string fileName)
        {
            return File.GetLastWriteTimeUtc(HttpContext.Current.Server.MapPath(resourcesFolder) + "/" + fileName);
        }

        public void Process(string[] files, string path, HttpContextBase context)
        {
            if (!string.IsNullOrEmpty(path)) resourcesFolder = "../" + path.Replace("%2f", "/");

            this.context = context;

            // Get last modification date of the set of requested files
            DateTime lmServer = DateTime.MinValue;
            foreach (string fileName in files)
            {
                DateTime fileLastModified = GetLastModifiedDate(fileName);
                if (fileLastModified > lmServer) lmServer = fileLastModified;
            }

            // If HTTP_IF_MODIFIED_SINCE header is present the browser has a previous version of the set
            // We check if the date it is sending is the same or after the las modification date, so we
            // do not need to resend the file, just tell the browser it has not been modified
            if (context.Request["HTTP_IF_MODIFIED_SINCE"] != null)
            {
                try
                {
                    DateTime lmBrowser = DateTime.Parse(context.Request["HTTP_IF_MODIFIED_SINCE"].ToString()).ToUniversalTime();

                    if (lmServer.Date == lmBrowser.Date && Math.Truncate(lmServer.TimeOfDay.TotalSeconds) <= lmBrowser.TimeOfDay.TotalSeconds)
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = (int)HttpStatusCode.NotModified;
                        context.Response.SuppressContent = true;
                        context.Response.Cache.SetCacheability(HttpCacheability.Public);
                        context.Response.Cache.SetExpires(DateTime.Now.Add(CACHE_DURATION));    //F5 will make the browser re-check the files
                        context.Response.Cache.SetMaxAge(CACHE_DURATION);
                        //context.Response.Cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
                        return;
                    }
                }
                catch (ApplicationException)
                {
                    //usually, datetime string couldn't be parsed to a datetime instance
                }
            }

            context.Response.Cache.SetLastModified(lmServer);

            // Decide if browser supports compressed response
            bool isCompressed = this.CanGZip(context.Request);

            // If the set has already been cached, write the response directly from
            // cache. Otherwise generate the response and cache it
            if (cacheable && !this.WriteFromCache(isCompressed, lmServer) || !cacheable)
            {
                using (MemoryStream memoryStream = new MemoryStream(8092))
                {
                    // Decide regular stream or gzip stream based on whether the response can be compressed or not
                    //using (Stream writer = isCompressed ?  (Stream)(new GZipStream(memoryStream, CompressionMode.Compress)) : memoryStream)
                    using (Stream writer = isCompressed ?
                                (Stream)(new GZipStream(memoryStream, CompressionMode.Compress)) :
                                memoryStream)
                    {

                        // Read the files into one big string
                        StringBuilder allScripts = new StringBuilder();
                        foreach (string fileName in files)
                        {
                            if (fileName.EndsWith(Extension.StartsWith(".") ? Extension : "." + Extension))
                            {
                                try
                                {
                                    allScripts.Append(ReadFile(fileName));
                                }
                                catch (Exception) { }
                            }
                        }
                        string minified = allScripts.ToString();
                        minified = Minify(minified);

                        // Send minfied string to output stream
                        byte[] bts = Encoding.UTF8.GetBytes(minified);
                        writer.Write(bts, 0, bts.Length);
                    }

                    // Cache the combined response so that it can be directly written
                    // in subsequent calls 
                    byte[] responseBytes = memoryStream.ToArray();
                    if (cacheable)
                    {
                        context.Cache.Insert(GetCacheKey(),
                        responseBytes, null, System.Web.Caching.Cache.NoAbsoluteExpiration,
                        CACHE_DURATION);

                        context.Cache.Insert(GetCacheKey() + lastModifiedDateKey,
                        lmServer, null, System.Web.Caching.Cache.NoAbsoluteExpiration,
                        CACHE_DURATION);
                    }

                    // Generate the response
                    this.WriteBytes(responseBytes, isCompressed);

                }
            }
        }

        private bool WriteFromCache(bool isCompressed, DateTime lastModificationDate)
        {
            byte[] responseBytes = context.Cache[GetCacheKey()] as byte[];

            if (responseBytes == null || responseBytes.Length == 0)
                return false;

            //Compare with the date of the server cache content
            DateTime lmd = (DateTime)context.Cache[GetCacheKey() + lastModifiedDateKey];
 
            if (lmd != lastModificationDate) return false;

            this.WriteBytes(responseBytes, isCompressed);
            return true;
        }

        private void WriteBytes(byte[] bytes, bool isCompressed)
        {
            HttpResponseBase response = context.Response;

            response.AppendHeader("Content-Length", bytes.Length.ToString());
            response.ContentType = contentType;

            if (isCompressed)
                response.AppendHeader("Content-Encoding", "gzip");
            else
                response.AppendHeader("Content-Encoding", "utf-8");

            response.AppendHeader("Vary", "Accept-Encoding");
            //response.Cache.SetVaryByCustom("Accept-Encoding");
            response.Cache.SetCacheability(HttpCacheability.Public);
            response.Cache.SetExpires(DateTime.Now.Add(CACHE_DURATION));
            response.Cache.SetMaxAge(CACHE_DURATION);
            //response.Cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
            response.ContentEncoding = Encoding.Unicode;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Flush();
        }

        private string GetCacheKey(string setName)
        {
            return "HttpCombiner." + setName;
        }

        private string GetCacheKey()
        {
            return "HttpCombiner." + GetUniqueKey(context);
        }

        private bool CanGZip(HttpRequestBase request)
        {
            string acceptEncoding = request.Headers["Accept-Encoding"];
            if (!string.IsNullOrEmpty(acceptEncoding) &&
                 (acceptEncoding.Contains("gzip") || acceptEncoding.Contains("deflate")))
                return true;
            return false;
        }

     

        /*  public static string GetScriptTags(string setName, int version)
          {
              string result = null;
  #if (DEBUG)
              foreach (string fileName in GetScriptFileNames(setName))
              {
                  result += String.Format("\n<script type=\"text/javascript\" src=\"{0}?v={1}\"></script>", VirtualPathUtility.ToAbsolute(fileName), version);
              }
  #else
          result += String.Format("<script type=\"text/javascript\" src=\"ScriptCombiner.axd?s={0}&v={1}\"></script>", setName, version);
  #endif
              return result;
          }*/


        public string GetUniqueKey(HttpContextBase context)
        {
            return context.Request.Url.PathAndQuery.ToUpperInvariant().GetHashCode().ToString("x", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    public class Common
    {
        static string version;
        public static string Version
        {
            get {
                if (version == null) {
                    System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(assembly.Location);
                    DateTime lastModified = fileInfo.LastWriteTime;
                    version = Base64Encode(lastModified.Ticks / TimeSpan.TicksPerSecond);
                }
                return version;
            }
        }
        public static string ReplaceRelativeImg(string content, string fileName)
        {

            string[] parts = fileName.Split('/');
            //replace relative paths

            Match m = Regex.Match(content, "(?<begin>url\\([\"\']?)(?<content>.+?)[\"\'\\)]+");

            StringBuilder sb = new StringBuilder();
            int firstIndex = 0;

            while (m.Success)
            {
                string relativePath = m.Groups["content"].ToString();

                int partsIndex = parts.Length - 1;

                while (relativePath.StartsWith("../"))
                {
                    partsIndex--;
                    relativePath = relativePath.Substring(3);
                }

                if (partsIndex < 0) break;

                StringBuilder sbPath = new StringBuilder();
                for (int i = 0; i < partsIndex; i++)
                {
                    sbPath.Append(parts[i]);
                    sbPath.Append("/");
                }
                sbPath.Append(relativePath);

                sb.Append(content.Substring(firstIndex, m.Index - firstIndex));
                sb.Append("url(\"{0}\")".Formato("../" + sbPath.ToString()));
                firstIndex = m.Index + m.Length;
                m = m.NextMatch();
            }
            sb.Append(content.Substring(firstIndex, content.Length - firstIndex));

            return sb.ToString();
        }

        public static string Base64Encode(long intNumber)
        {
            long intNum = default(long);
            string strSum = null;
            long intCarry = default(long);
            long intConvertBase = 62;
            strSum = "";
            intNum = intNumber;
            do
            {
                intCarry = intNum % intConvertBase;
                if (intCarry > 35)
                {
                    strSum = Convert.ToChar(intCarry - 35 + 96) + strSum;
                }

                else if (intCarry > 9)
                {

                    strSum = Convert.ToChar(intCarry - 9 + 64) + strSum;
                }
                else
                {
                    strSum = intCarry + strSum;
                }
                intNum = (long)(intNum / intConvertBase);
            }
            while (!(intNum == 0));
            return strSum;
        }

    }
}