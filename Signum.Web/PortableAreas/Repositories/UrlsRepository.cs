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
                { "popupView", url => url.SignumAction("PopupView") },
                { "partialView", url => url.SignumAction("PartialView") },
                { "validate", url => url.SignumAction("Validate") },
                { "find", url => url.SignumAction("Find") },
                { "partialFind", url => url.SignumAction("PartialFind") },
                { "search", url => url.SignumAction("Search") },
                { "subTokensCombo", url => url.SignumAction("NewSubTokensCombo") },
                { "addFilter", url => url.Action("AddFilter", "Signum") },
                { "quickFilter", url => url.SignumAction("QuickFilter") },
                { "selectedItemsContextMenu", url => url.SignumAction("SelectedItemsContextMenu") },
                { "create", url => url.SignumAction("Create") },
                { "view", url => url.SignumAction("View") },
                { "popupNavigate", url => url.SignumAction("PopupNavigate") },
                { "normalControl", url => url.SignumAction("NormalControl") },
                { "autocomplete", url => url.SignumAction("Autocomplete") }
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
                    sw.WriteLine(DefaultSFUrls.ToString(kvp => "{0}:'{1}'".Formato(kvp.Key, kvp.Value), ", "));
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