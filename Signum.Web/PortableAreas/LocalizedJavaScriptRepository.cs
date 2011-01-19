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

namespace Signum.Web.PortableAreas
{
    public class LocalizedJavaScriptRepository : IFileRepository
    {
        public readonly ResourceManager ResourceManager;
        public readonly string VirtualPath;
        readonly string Folder;
        readonly string ResourceKeyPrefix;
        readonly string JavaScriptVariableName;

        string TotalPrefix { get { return VirtualPath + Folder; } }

        readonly ConcurrentDictionary<CultureInfo, StaticContentResult> cachedFiles = new ConcurrentDictionary<CultureInfo, StaticContentResult>();

        public LocalizedJavaScriptRepository(ResourceManager resourceManager, string areaName)
            : this(resourceManager, "~/" + areaName + "/", "resources/", areaName + "_", areaName)
        {
        }

        public LocalizedJavaScriptRepository(ResourceManager resourceManager, string virtualPath, string folder, string resourceKeyPrefix, string javaScriptVariableName)
        {
            if (resourceManager == null)
                throw new ArgumentNullException("resourceManager");

            if (string.IsNullOrEmpty(virtualPath))
                throw new ArgumentNullException("virtualPath");

            if (string.IsNullOrEmpty(resourceKeyPrefix))
                throw new ArgumentNullException("resourceKeyPrefix");

            this.ResourceManager = resourceManager;
            this.VirtualPath = virtualPath.ToLower();
            this.Folder = folder.ToLower();
            this.ResourceKeyPrefix = resourceKeyPrefix.ToLower();
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
                    sw.WriteLine("lang.{0} =".Formato(JavaScriptVariableName));
                    sw.WriteLine("{");

                    sw.WriteLine(string.Join(",\r\n", dic.Select(p => "   {0}: {1}".Formato(p.Key, p.Value.Quote()))));

                    sw.WriteLine("};");
                }

                return ms.ToArray();
            }
        }

        Dictionary<string, string> ReadAllKeys(CultureInfo ci)
        {
            ResourceSet set = ResourceManager.GetResourceSet(ci, true, true);

            var dict = set.Cast<DictionaryEntry>()
                .Where(e => e.Key.ToString().StartsWith(ResourceKeyPrefix, StringComparison.InvariantCultureIgnoreCase))
                .ToDictionary(e => e.Key.ToString().Substring(ResourceKeyPrefix.Length), e => e.Value.ToString());


            if (ci == CultureInfo.InvariantCulture)
                return dict;

            var baseDict = ReadAllKeys(ci.Parent);

            baseDict.SetRange(dict);

            return baseDict;
        }

        public bool FileExists(string file)
        {
            return GetCultureInfo(file) != null;
        }

        CultureInfo GetCultureInfo(string virtualPath)
        {
            if (!virtualPath.StartsWith(TotalPrefix, StringComparison.InvariantCultureIgnoreCase))
                return null;

            var fileName = virtualPath.Substring(TotalPrefix.Length);

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
            return "LocalizedJavaScript {0} -> {1}".Formato(ResourceKeyPrefix, TotalPrefix);
        }
    }
}