using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using Microsoft.Win32;
using Signum.Utilities;
using System.Collections.Concurrent;

namespace Signum.Web
{
    public static class MimeType
    {
        public static ConcurrentDictionary<string, string> Cache = new ConcurrentDictionary<string,string>(new Dictionary<string,string>
        {
            { ".txt", "application/plain-text" },
            { ".doc", "application/msword" },
            { ".dot", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".dotx", "application/vnd.openxmlformats-officedocument.wordprocessingml.template" },
            { ".docm", "application/vnd.ms-word.document.macroEnabled.12" },
            { ".dotm", "application/vnd.ms-word.template.macroEnabled.12" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlt", "application/vnd.ms-excel" },
            { ".xla", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".xltx", "application/vnd.openxmlformats-officedocument.spreadsheetml.template" },
            { ".xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12" },
            { ".xltm", "application/vnd.ms-excel.template.macroEnabled.12" },
            { ".xlam", "application/vnd.ms-excel.addin.macroEnabled.12" },
            { ".xlsb", "application/vnd.ms-excel.sheet.binary.macroEnabled.12" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".pot", "application/vnd.ms-powerpoint" },
            { ".pps", "application/vnd.ms-powerpoint" },
            { ".ppa", "application/vnd.ms-powerpoint" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".potx", "application/vnd.openxmlformats-officedocument.presentationml.template" },
            { ".ppsx", "application/vnd.openxmlformats-officedocument.presentationml.slideshow" },
            { ".ppam", "application/vnd.ms-powerpoint.addin.macroEnabled.12" },
            { ".pptm", "application/vnd.ms-powerpoint.presentation.macroEnabled.12" },
            { ".potm", "application/vnd.ms-powerpoint.presentation.macroEnabled.12" },
            { ".ppsm", "application/vnd.ms-powerpoint.slideshow.macroEnabled.12" },
        }); 

        public static string FromFileName(string fileName)
        {
            return FromExtension(Path.GetExtension(fileName)); 
        }

        public static string FromExtension(string extension)
        {
            extension = extension.ToLower();

            return Cache.GetOrAdd(extension, ext =>
            {
                RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(ext);
                if (regKey != null && regKey.GetValue("Content Type") != null)
                    return regKey.GetValue("Content Type").ToString();


                return "application/" + extension.Substring(1);
            }); 
        }
    }
}