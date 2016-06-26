using Signum.Engine.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;

namespace Signum.React.Translation
{
    public class TranslationServer
    {
        public static void Start(HttpConfiguration config, bool copyNewTranslationsToRootFolder = true)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            if (copyNewTranslationsToRootFolder)
            {
                string path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(new Uri(typeof(DescriptionManager).Assembly.CodeBase).LocalPath)), "Translations");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var existingFiles = Directory.GetFiles(path).ToHashSet();

                foreach (string fromFile in Directory.GetFiles(DescriptionManager.TranslationDirectory))
                {
                    string toFile = Path.Combine(path, Path.GetFileName(fromFile));

                    if (!existingFiles.Contains(toFile) || File.GetLastWriteTime(toFile) < File.GetLastWriteTime(fromFile))
                    {
                        File.Copy(fromFile, toFile, overwrite: true);
                    }
                }

                DescriptionManager.TranslationDirectory = path;
            }
        }

        public static CultureInfo GetCultureRequest(HttpRequest request)
        {
            if (request.UserLanguages == null)
                return null;

            foreach (string lang in request.UserLanguages)
            {
                string cleanLang = lang.Contains('-') ? lang.Split('-')[0] : lang;

                var culture = CultureInfoLogic.ApplicationCultures
                    .Where(ci => ci.Name.StartsWith(cleanLang))
                    .FirstOrDefault();

                if (culture != null)
                    return culture;
            }

            return null;
        }
    }
}