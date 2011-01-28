using System.IO;
using System.Net;
using System.Web;
using System.Xml;
using System;

namespace Signum.Web.Combine
{
    /// <summary>
    /// A C# wrapper around the Google Closure Compiler web service.
    /// </summary>
    public static class GoogleClosure
    {
        private const string PostData = "js_code={0}&output_format=xml&output_info=compiled_code&compilation_level=SIMPLE_OPTIMIZATIONS";
        private const string ApiEndpoint = "http://closure-compiler.appspot.com/compile";


        public static string CompressSourceCode(string source)
        {
            try
            {
                XmlDocument xml = CallApi(source);
                return xml.SelectSingleNode("//compiledCode").InnerText;
            }
            catch (Exception)
            {
                return source;
            }
        }

        static XmlDocument CallApi(string source)
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