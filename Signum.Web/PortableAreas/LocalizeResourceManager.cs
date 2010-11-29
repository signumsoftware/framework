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

namespace Signum.Web
{
    public static class LocalizeResourceManager
    {
        private static Dictionary<string, LocalizeResourceStore> localizeResourceStores = new Dictionary<string, LocalizeResourceStore>();

        public static VirtualFile GetVirtualFile(string virtualPath)
        {
            string prefix = GetPrefix(virtualPath);

            if (prefix == null)
                return null;

            var store = localizeResourceStores.TryGetC(prefix.ToLower());
            if (store == null)
                return null;

            if (store.FileExists(virtualPath))
                return new LocalizeResourceVirtualFile(virtualPath, store);

            return null;
        }

        public static bool FileExist(string virtualPath)
        {
            string prefix = GetPrefix(virtualPath);

            if (prefix == null)
                return false;

            var store = localizeResourceStores.TryGetC(prefix.ToLower());
            if (store == null)
                return false;

            return store.FileExists(virtualPath);
        }

        static string GetPrefix(string virtualPath)
        {
            if (!virtualPath.StartsWith("/"))
                return null;

            int index = virtualPath.IndexOf('/', 1);
            if (index == -1)
                return null;

            return virtualPath.Substring(0, index + 1);
        }

        public static void RegisterAreaResources(LocalizeResourceStore store)
        {
            localizeResourceStores.Add(store.VirtualPath, store);
        }

        class LocalizeResourceVirtualFile : VirtualFile
        {
            public readonly LocalizeResourceStore store;

            public LocalizeResourceVirtualFile(string virtualPath, LocalizeResourceStore store)
                : base(virtualPath)
            {
                this.store = store;
            }

            public override Stream Open()
            {
                return this.store.GetResourceStream(this.VirtualPath);
            }
        }
    }

    public class LocalizeResourceStore
    {
        public readonly ResourceManager ResourceManager;
        public readonly string VirtualPath;
        readonly string Folder; 
        readonly string ResourceKeyPrefix;
        readonly string JavaScriptVariableName;

        string TotalPrefix { get { return VirtualPath + Folder; } }

        readonly ConcurrentDictionary<CultureInfo, byte[]> cachedFiles = new ConcurrentDictionary<CultureInfo,byte[]>();

        public LocalizeResourceStore(ResourceManager resourceManager, string areaName)
            : this(resourceManager, "/" + areaName + "/", "resources/", areaName + "_", areaName)
        {
        }

        public LocalizeResourceStore(ResourceManager resourceManager, string virtualPath, string folder, string resourceKeyPrefix, string javaScriptVariableName)
        {
            if (resourceManager == null)
                throw new ArgumentNullException("resourceManager");

            if (string.IsNullOrEmpty(virtualPath))
                throw new ArgumentNullException("virtualPath");

            if (string.IsNullOrEmpty(resourceKeyPrefix))
                throw new ArgumentNullException("resourceKeyPrefix");

            this.ResourceManager= resourceManager;
            this.VirtualPath = virtualPath.ToLower();
            this.Folder = folder.ToLower(); 
            this.ResourceKeyPrefix = resourceKeyPrefix.ToLower();
            this.JavaScriptVariableName = javaScriptVariableName.ToLower();
        }

        public Stream GetResourceStream(string virtualPath)
        {   
            CultureInfo culture = GetCultureInfo(virtualPath); 

            if(culture == null)
                return null; 

            byte[] bytes = this.cachedFiles.GetOrAdd(culture, ci=>CreateFile(ci));

            return new MemoryStream(bytes); 
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
                    foreach (var p in dic)
                    {
                        sw.WriteLine("   {0}: {1},".Formato(p.Key, p.Value.Quote()));
                    }
                    sw.WriteLine("};");
                }

                return ms.ToArray();
            }

        }

        private Dictionary<string, string> ReadAllKeys(CultureInfo ci)
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

        public bool FileExists(string virtualPath)
        {
            return GetCultureInfo(virtualPath)!= null;
        }

        private CultureInfo GetCultureInfo(string virtualPath)
        {
            virtualPath = virtualPath.ToLower();

            if (!virtualPath.StartsWith(VirtualPath))
                throw new InvalidOperationException("virtualPath is not from this store");

            if (!virtualPath.StartsWith(TotalPrefix))
                return null;

            var fileName = virtualPath.Substring(TotalPrefix.Length);

            if(Path.GetExtension(fileName)!= ".js")
                return null;

            try
            {
                return CultureInfo.GetCultureInfo(Path.GetFileNameWithoutExtension(fileName));
            }
            catch(CultureNotFoundException)
            {
                return null;
            }
        }
    }
}