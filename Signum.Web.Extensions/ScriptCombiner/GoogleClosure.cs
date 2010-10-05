using System.IO;
using System.Net;
using System.Web;
using System.Xml;
using System;

namespace Signum.Web.ScriptCombiner
{
    /// <summary>
    /// A C# wrapper around the Google Closure Compiler web service.
    /// </summary>
    public static class GoogleClosure
    {
        private const string PostData = "js_code={0}&output_format=xml&output_info=compiled_code&compilation_level=SIMPLE_OPTIMIZATIONS";
        private const string ApiEndpoint = "http://closure-compiler.appspot.com/compile";

        /// <summary>
        /// Compresses the specified file using Google's Closure Compiler algorithm.
        /// <remarks>
        /// The file to compress must be smaller than 200 kilobytes.
        /// </remarks>
        /// </summary>
        /// <param name="file">The absolute file path to the javascript file to compress.</param>
        /// <returns>A compressed version of the specified JavaScript file.</returns>
        public static string Compress(string file)
        {
            string source = File.ReadAllText(file);
            XmlDocument xml = CallApi(source);
            return xml.SelectSingleNode("//compiledCode").InnerText;
        }

        public static string CompressSourceCode(string source)
        {
            try
            {
                XmlDocument xml = CallApi(source);
                return xml.SelectSingleNode("//compiledCode").InnerText;
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Calls the API with the source file as post data.
        /// </summary>
        /// <param name="source">The content of the source file.</param>
        /// <returns>The Xml response from the Google API.</returns>
        private static XmlDocument CallApi(string source)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("content-type", "application/x-www-form-urlencoded");
                string data = string.Format(PostData, HttpUtility.UrlEncode(source));
                string result = client.UploadString(ApiEndpoint, data);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(result);
                return doc;
            }
        }
    }
}