using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.Translation;
using Signum.Entities;
using Signum.Entities.Translation;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Web.Basic;
using Signum.Web.Omnibox;
using Signum.Web.PortableAreas;
using Signum.Web.Translation.Controllers;
using Signum.Web.Cultures;

namespace Signum.Web.Translation
{
    public static class TranslationClient
    {
        public static string ViewPrefix = "~/Translation/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Translation/Scripts/Translation");

        public static ITranslator Translator; 


        /// <param name="copyTranslationsToRootFolder">avoids Web Application restart when translations change</param>
        public static void Start(ITranslator translator, bool translatorUser, bool translationReplacement, bool instanceTranslator, bool copyNewTranslationsToRootFolder = true)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoClient.Start();

                Translator = translator;

                Navigator.RegisterArea(typeof(TranslationClient));

                if (translatorUser)
                {
                    Navigator.AddSettings(new List<EntitySettings>
                    {
                        new EntitySettings<TranslatorUserEntity>{ PartialViewName = t=>ViewPrefix.FormatWith("TranslatorUser")},
                        new EmbeddedEntitySettings<TranslatorUserCultureEntity>{ PartialViewName = t=>ViewPrefix.FormatWith("TranslatorUserCulture")},
                    });
                }

                if (translationReplacement)
                {
                    FileRepositoryManager.Register(new LocalizedJavaScriptRepository(typeof(TranslationJavascriptMessage), "translation"));

                    Navigator.AddSettings(new List<EntitySettings>
                    {
                        new EntitySettings<TranslationReplacementEntity>{ PartialViewName = t=>ViewPrefix.FormatWith("TranslationReplacement")},
                    });
                }

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("TranslateCode",
                    () => TranslationPermission.TranslateCode.IsAuthorized(),
                    uh => uh.Action((TranslationController tc) => tc.Index())));

                if (instanceTranslator)
                {
                    SpecialOmniboxProvider.Register(new SpecialOmniboxAction("TranslateInstances",
                        () => TranslationPermission.TranslateInstances.IsAuthorized(),
                        uh => uh.Action((TranslatedInstanceController tic) => tic.Index())));
                }

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

        public static SelectListItem ToTranslatedSelectListItem<T>(this Lite<T> lite, Lite<T> selected, Expression<Func<T, string>> toStringField) where T : Entity
        {
            return new SelectListItem { Text = lite.TranslatedField(toStringField, lite.ToString()), Value = lite.Id.ToString(), Selected = lite.Equals(selected) };
        }

        public static MvcHtmlString Diff(string oldStr, string newStr)
        {
            StringDistance sd = new StringDistance();

            var dif = sd.DiffText(oldStr, newStr);

            HtmlStringBuilder sb = new HtmlStringBuilder();
            foreach (var line in dif)
            {
                if (line.Action == StringDistance.DiffAction.Removed)
                {
                    using (sb.Surround(new HtmlTag("span").Attr("style", "background-color:#FFD1D1")))
                        DiffLine(sb, line.Value);
                }
                if (line.Action == StringDistance.DiffAction.Added)
                {
                    using (sb.Surround(new HtmlTag("span").Attr("style", "background-color:#CEF3CE")))
                        DiffLine(sb, line.Value);
                }
                else if (line.Action == StringDistance.DiffAction.Equal)
                {
                    if (line.Value.Count == 1)
                    {
                        using (sb.Surround(new HtmlTag("span")))
                            DiffLine(sb, line.Value);
                    }
                    else
                    {
                        using (sb.Surround(new HtmlTag("span").Attr("style", "background-color:#FFD1D1")))
                            DiffLine(sb, line.Value.Where(a => a.Action == StringDistance.DiffAction.Removed || a.Action == StringDistance.DiffAction.Equal));

                        using (sb.Surround(new HtmlTag("span").Attr("style", "background-color:#CEF3CE")))
                            DiffLine(sb, line.Value.Where(a => a.Action == StringDistance.DiffAction.Added || a.Action == StringDistance.DiffAction.Equal));
                    }
                }
            }

            return sb.ToHtml();
        }

        private static void DiffLine(HtmlStringBuilder sb, IEnumerable<StringDistance.DiffPair<string>> list)
        {
            foreach (var gr in list.GroupWhenChange(a=>a.Action))
            {
                string text = gr.Select(a => a.Value).ToString("");

                if (gr.Key == StringDistance.DiffAction.Equal)
                    sb.Add(HtmlTag.Encode(text));
                else
                {
                    var color =
                        gr.Key == StringDistance.DiffAction.Added ? "#72F272" :
                        gr.Key == StringDistance.DiffAction.Removed ? "#FF8B8B" :
                        new InvalidOperationException().Throw<string>();

                    sb.Add(new HtmlTag("span").Attr("style", "background:" + color).SetInnerText(text));
                }
            }

            sb.AddLine();
        }
    }
}