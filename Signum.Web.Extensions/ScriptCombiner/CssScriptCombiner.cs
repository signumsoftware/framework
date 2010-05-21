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

namespace Signum.Web.ScriptCombiner
{
    public class CssScriptCombiner : ScriptCombiner {
        public CssScriptCombiner() {
            this.contentType = "text/css";
            this.cacheable = true;
            this.gzipable = true;
            this.resourcesFolder = "../Content";
        }

        protected override string Minify(string content) {
            content = Regex.Replace(content, "/\\*.+?\\*/", "", RegexOptions.Singleline);
            content = Regex.Replace(content,"(\\s{2,}|\\t+|\\r+|\\n+)", string.Empty);
            //content = content.Replace(Environment.NewLine + Environment.NewLine + Environment.NewLine, string.Empty);
            //content = content.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
            //content = content.Replace(Environment.NewLine, string.Empty);
            //content = content.Replace("\\t", string.Empty);
           // content = content.Replace("\t", string.Empty);
            content = content.Replace(" {", "{");
            content = content.Replace("{ ", "{");
            content = content.Replace(" :", ":");
            content = content.Replace(": ", ":");
            content = content.Replace(", ", ",");
            content = content.Replace("; ", ";");
            content = content.Replace(";}", "}");
            content = Regex.Replace(content, "/\\*[^\\*]*\\*+([^/\\*]*\\*+)*/", "$1");
            content = Regex.Replace(content, "(?<=[>])\\s{2,}(?=[<])|(?<=[>])\\s{2,}(?=&nbsp;)|(?<=&ndsp;)\\s{2,}(?=[<])", string.Empty);
            //content = content.Replace("../", "../Content/");   //Eliminamos rutas relativas
            //content = content.Replace("../", new System.Web.Mvc.UrlHelper(.Content("~/Content"));
            content = Regex.Replace(content, "[^\\}]+\\{\\}", string.Empty);  //Eliminamos reglas vacías

            Regex color = new Regex("#([A-Fa-f0-9]{6})");
            foreach (Match CurrentMatch in color.Matches(content))
            {
                string coincidencia = CurrentMatch.Groups[1].Value;
                if (coincidencia[0] == coincidencia[1]
                    && coincidencia[2] == coincidencia[3]
                    && coincidencia[4] == coincidencia[5])
                    content = content.Replace("#" + coincidencia, ("#" + coincidencia[0] + coincidencia[2] + coincidencia[4]).ToLower());
            }
            return content;
        }
        public override string ProcessFile(string fileName)
        {

            return File.ReadAllText(fileName);

            //string[] folders = fileName.Replace("\\\\", "\\").Split('\\');
            ////allScripts.Append(File.ReadAllText(context.Server.MapPath(this.resourcesFolder + "/" + fileName.Replace("%2f", "/"))));

            //string content = File.ReadAllText(fileName);
            ////convert relative paths to absolute paths
            ////we get all the paths
            //string pattern = @"url\(([^\)]*)\)";
            //Match match = Regex.Match(content, pattern);

            //while (match.Success){
            //    string path=match.Groups[1].Value;
            //    int parents=0;
            //    while (path.StartsWith("../")){
            //        parents++;
            //        path = path.Substring(3);
            //    }
            //    string absolutePath = String.Empty;
            //    for (int i=0; i<(folders.Length-1-parents);i++){
            //        absolutePath += folders[i] + "/";
            //    }
            //    content = content.Replace(match.Groups[0].Value, "url(" + absolutePath + path + ")");
            //    match = Regex.Match(content, pattern);
            //}
            //return String.Empty;

        }

        protected override string Extension { get { return "css"; } }
    }

    public class JSScriptCombiner : ScriptCombiner {
        public JSScriptCombiner()
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
            return minifier.Minify(content);
        }
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
        protected abstract string Extension{get;}

        protected abstract string Minify(string content);

        private readonly static TimeSpan CACHE_DURATION = TimeSpan.FromDays(2);
        private HttpContextBase context;

        public virtual string ProcessFile(string fileName)
        {
            return File.ReadAllText(fileName);
        }

        public void Process(string[] files, string path, HttpContextBase context)
        {
            this.version = "1.1";
            if (path != null) resourcesFolder = "../" + path.Replace("%2f", "/");

            this.context = context;
            DateTime lmServer = DateTime.MinValue;
            foreach (string fileName in files)
            {
                string file = context.Server.MapPath(resourcesFolder + "/" + fileName.Replace("%2f", "/"));
                DateTime fileLastModified = File.GetLastWriteTimeUtc(file);
                if (fileLastModified > lmServer) lmServer = fileLastModified;
            }

            //check dates
            if (context.Request["HTTP_IF_MODIFIED_SINCE"] != null)
            {
                DateTime lmBrowser = DateTime.Parse(context.Request["HTTP_IF_MODIFIED_SINCE"].ToString()).ToUniversalTime();

                if (Math.Truncate(lmServer.TimeOfDay.TotalSeconds) <= lmBrowser.TimeOfDay.TotalSeconds)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = (int)HttpStatusCode.NotModified;
                    context.Response.SuppressContent = true;
                    return;
                }
            }

            context.Response.Cache.SetLastModified(lmServer);

            // Decide if browser supports compressed response
            bool isCompressed = this.CanGZip(context.Request);

            // If the set has already been cached, write the response directly from
            // cache. Otherwise generate the response and cache it
            if (cacheable && !this.WriteFromCache(version, isCompressed) || !cacheable)
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
                                    //allScripts.Append(File.ReadAllText(context.Server.MapPath(fileName)));
                                    string file = context.Server.MapPath(resourcesFolder + "/" + fileName.Replace("%2f", "/"));
                                    allScripts.Append(ProcessFile(file));
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
                        context.Cache.Insert(GetCacheKey(version),
                        responseBytes, null, System.Web.Caching.Cache.NoAbsoluteExpiration,
                        CACHE_DURATION);


                    // Generate the response
                    this.WriteBytes(responseBytes, isCompressed);

                }
            }
        }

        private bool WriteFromCache(string setName, string version, bool isCompressed)
        {
            byte[] responseBytes = context.Cache[GetCacheKey(setName, version)] as byte[];

            if (responseBytes == null || responseBytes.Length == 0)
                return false;

            this.WriteBytes(responseBytes, isCompressed);
            return true;
        }

        private bool WriteFromCache(string version, bool isCompressed)
        {
            byte[] responseBytes = context.Cache[GetCacheKey(version)] as byte[];

            if (responseBytes == null || responseBytes.Length == 0)
                return false;

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

            //response.Cache.SetCacheability(HttpCacheability.Public);
            //response.Cache.SetExpires(DateTime.Now.Add(CACHE_DURATION));
            //response.Cache.SetMaxAge(CACHE_DURATION);
            //response.Cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
            response.ContentEncoding = Encoding.Unicode;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Flush();
        }

        private string GetCacheKey(string setName, string version)
        {
            return "HttpCombiner." + setName + "." + version;
        }

        private string GetCacheKey(string version)
        {
            return "HttpCombiner." + GetUniqueKey(context) + "." + version;
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
}