using Signum.Engine.Basics;
using Signum.Engine.Translation;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using Signum.React.Facades;
using Signum.Entities.Translation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Signum.React.Translation
{
    public class TranslationServer
    {
        public static ITranslator Translator;

        public static void Start(IApplicationBuilder app, ITranslator translator, bool copyNewTranslationsToRootFolder = true)
        {
            ReflectionServer.RegisterLike(typeof(TranslationMessage));

            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
            Translator = translator;

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

        public static CultureInfo GetCultureRequest(HttpRequestMessage request)
        {
            foreach (string lang in request.Headers.AcceptLanguage.Select(a => a.Value))
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
        

        public static string ReadLanguageCookie(ActionContext ac)
        {
            return ac.HttpContext.Request.Cookies.TryGetValue("language", out string value) ? value : null;
        }
    }
}