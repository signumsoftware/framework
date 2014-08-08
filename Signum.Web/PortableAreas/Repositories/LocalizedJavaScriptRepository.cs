using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Signum.Utilities;
using System.Web.Hosting;
using System.Globalization;
using System.Text;
using System.Collections;
using System.Resources;
using System.Collections.Concurrent;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace Signum.Web.PortableAreas
{
    public class LocalizedJavaScriptRepository : IFileRepository
    {
        public readonly Type MessageType;
        public readonly string VirtualPathPrefix;
        readonly string JavaScriptVariableName;

        readonly ConcurrentDictionary<CultureInfo, StaticContentResult> cachedFiles = new ConcurrentDictionary<CultureInfo, StaticContentResult>();

        public LocalizedJavaScriptRepository(Type messageType, string areaName)
            : this(messageType, "~/" + areaName + "/resources/", areaName)
        {
        }

        public LocalizedJavaScriptRepository(Type messageType, string virtualPathPrefix, string javaScriptVariableName)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");

            if (string.IsNullOrEmpty(virtualPathPrefix))
                throw new ArgumentNullException("virtualPath");

            this.MessageType = messageType;
            this.VirtualPathPrefix = virtualPathPrefix.ToLower();
            this.JavaScriptVariableName = javaScriptVariableName.ToLower();
        }

        public ActionResult GetFile(string file)
        {
            CultureInfo culture = GetCultureInfo(file);

            if (culture == null)
                return null;

            return this.cachedFiles.GetOrAdd(culture, ci => new StaticContentResult(CreateFile(ci), file));
        }

        byte[] CreateFile(CultureInfo ci)
        {
            var dic = ReadAllKeys(ci);

            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8))
                {
                    sw.WriteLine("if(typeof lang == 'undefined' || lang == null) lang = {};");
                    sw.WriteLine("lang.{0} = {1};".Formato(JavaScriptVariableName, JsonConvert.SerializeObject(dic, Formatting.Indented)));
                }

                return ms.ToArray();
            }
        }

        Dictionary<string, string> ReadAllKeys(CultureInfo ci)
        {
            using (CultureInfoUtils.ChangeCultureUI(ci))
                return Enum.GetValues(MessageType).Cast<Enum>().ToDictionary(a => a.ToString(), a => a.NiceToString());
        }

        public bool FileExists(string file)
        {
            return GetCultureInfo(file) != null;
        }

        CultureInfo GetCultureInfo(string virtualPath)
        {
            if (!virtualPath.StartsWith(VirtualPathPrefix, StringComparison.InvariantCultureIgnoreCase))
                return null;

            var fileName = virtualPath.Substring(VirtualPathPrefix.Length);

            if (Path.GetExtension(fileName) != ".js")
                return null;

            try
            {
                return CultureInfo.GetCultureInfo(Path.GetFileNameWithoutExtension(fileName));
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
        }

        public override string ToString()
        {
            return "LocalizedJavaScript {0} -> {1}".Formato(MessageType.Name, VirtualPathPrefix);
        }
    }
}