using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;

namespace Signum.Web.PortableAreas
{
    public class UrlsRepository: IFileRepository
    { 
        public static Dictionary<string, Func<UrlHelper, string>> DefaultSFUrls = new Dictionary<string, Func<UrlHelper, string>>
        {
            { "popupView", url => url.Action("PopupView", "Navigator") },
            { "partialView", url => url.Action("PartialView", "Navigator") },
            { "create", url => url.Action("Create", "Navigator") },
            { "view", url => url.Action("View", "Navigator") },
            { "popupNavigate", url => url.Action("PopupNavigate", "Navigator") },
            { "normalControl", url => url.Action("NormalControl", "Navigator") },
            { "valueLineBox", url => url.Action("ValueLineBox", "Navigator") },

            { "validate", url => url.Action("Validate", "Validator") },

            { "find", url => url.Action("Find", "Finder") },
            { "partialFind", url => url.Action("PartialFind", "Finder") },
            { "search", url => url.Action("Search", "Finder") },
            { "subTokensCombo", url => url.Action("NewSubTokensCombo", "Finder") },
            { "addFilter", url => url.Action("AddFilter", "Finder") },
            { "quickFilter", url => url.Action("QuickFilter", "Finder") },
            { "selectedItemsContextMenu", url => url.Action("SelectedItemsContextMenu", "Finder") },

            { "autocomplete", url => url.Action("Autocomplete", "Finder") }
        };

        
        public static ResetLazy<StaticContentResult> FileLazy; 

        public readonly string VirtualPathPrefix;
        
        public UrlsRepository(string virtualPathPrefix)
        {
            if (string.IsNullOrEmpty(virtualPathPrefix))
                throw new ArgumentNullException("virtualPath");

            this.VirtualPathPrefix = virtualPathPrefix.ToLower();

            FileLazy = new ResetLazy<StaticContentResult>(()=>new StaticContentResult(CreateFile(), fileName));
        }

        static readonly string fileName = "signumCommon.js";

        public ActionResult GetFile(string file)
        {
            if (!FileExists(file))
                throw new FileNotFoundException();

            return FileLazy.Value;

        }

        byte[] CreateFile()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8))
                {
                    sw.WriteLine("var SF = SF || {}; ");
                    sw.WriteLine("SF.Urls = $.extend(SF.Urls || {}, { ");
                    var helper = RouteHelper.New();
                    sw.WriteLine(DefaultSFUrls.ToString(kvp => "{0}:'{1}'".Formato(kvp.Key, kvp.Value(helper)), ", "));
                    sw.WriteLine("});");
                }

                return ms.ToArray();
            }
        }
    
        public bool FileExists(string file)
        {
            if (!file.StartsWith(VirtualPathPrefix, StringComparison.InvariantCultureIgnoreCase))
                return false;

            var fileName = file.Substring(VirtualPathPrefix.Length);

            if (fileName.Equals("signumCommon.js", StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public override string ToString()
        {
            return "UrlsRepository {0}".Formato(VirtualPathPrefix);
        }
    }
}