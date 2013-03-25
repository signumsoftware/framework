using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Signum.Engine.Translation;
using Signum.Entities.Authorization;
using Signum.Entities.Translation;
using Signum.Utilities;

namespace Signum.Web.Translation.Controllers
{
    public class TranslationController : Controller
    {
        public static IEnumerable<Assembly> AssembliesToLocalize()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(a => a.HasAttribute<DefaultAssemblyCultureAttribute>());
        }

        public ActionResult Index()
        {
            var cultures = CultureInfos("en");

            var dic = AssembliesToLocalize().ToDictionary(a => a,
                a => cultures.Select(ci => new TranslationFile
                {
                    Assembly = a,
                    CultureInfo = ci,
                    FileName = LocalizedAssembly.TranslationFileName(a, ci)
                }).ToDictionary(tf => tf.CultureInfo));

            return base.View(TranslationClient.ViewPrefix.Formato("Index"), dic);
        }

        private static List<CultureInfo> CultureInfos(string defaultCulture)
        {
            var cultures = CultureInfoLogic.ApplicationCultures;

            TranslatorDN tr = UserDN.Current.Translator();

            if (tr != null)
                cultures = cultures.Where(ci => ci.Name == defaultCulture || tr.Cultures.Any(tc => tc.Culture.CultureInfo == ci));

            return cultures.OrderByDescending(a => a.Name == defaultCulture)
                    .ThenBy(a => a.Name).ToList();
        }

        public new ActionResult View(string assembly, string culture)
        {
            Assembly ass = AssembliesToLocalize().Where(a => a.GetName().Name == assembly).SingleEx(() => "Assembly {0} not found".Formato(assembly));

            CultureInfo defaultCulture = CultureInfo.GetCultureInfo(ass.SingleAttribute<DefaultAssemblyCultureAttribute>().DefaultCulture);

            Dictionary<CultureInfo, LocalizedAssembly> reference = (from ci in CultureInfos(defaultCulture.Name)
                                                                    let la = DescriptionManager.GetLocalizedAssembly(ass, ci)
                                                                    where la != null || ci == defaultCulture
                                                                    select KVP.Create(ci, la ?? LocalizedAssembly.ImportXml(ass, ci))).ToDictionary();

            ViewBag.Assembly = ass;
            ViewBag.DefaultCulture = defaultCulture;
            ViewBag.Culture = culture == null ? null : CultureInfo.GetCultureInfo(culture);

            return base.View(TranslationClient.ViewPrefix.Formato("View"), reference);
        }

        [HttpPost]
        public ActionResult View(string assembly, string culture, string bla)
        {   
            var currentAssembly = AssembliesToLocalize().Single(a => a.GetName().Name == assembly);

            var locAssemblies = (culture.HasText() ? new[] { CultureInfo.GetCultureInfo(culture) } : CultureInfos("en").ToArray())
                .ToDictionary(ci => ci.Name, ci => LocalizedAssembly.ImportXml(currentAssembly, ci));

            var list = (from k in Request.Form.AllKeys
                        let m = Regex.Match(k, @"(?<type>[_\w][_\w\d]*)\.(?<lang>[\w_\-]+)\.(?<kind>\w+)(\.(?<member>[_\w][_\w\d]*))?")
                        where m.Success
                        select new
                        {
                            Type = m.Groups["type"].Value,
                            Lang = m.Groups["lang"].Value,
                            Kind = m.Groups["kind"].Value,
                            Member = m.Groups["member"].Value,
                            Value = Request.Form[k],
                        }).ToList();

            var groups = list.AgGroupToDictionary(a => a.Lang, gr => gr.AgGroupToDictionary(a => a.Type, gr2 => gr2.ToList()));

            groups.JoinDictionaryForeach(locAssemblies, (cn, dic, la) =>
                {
                    dic.JoinDictionaryForeach(la.Types.Values.ToDictionary(a => a.Type.Name), (tn, tuples, lt) =>
                    {
                        foreach (var t in tuples)
                        {
                            switch (t.Kind)
                            {
                                case "Description": lt.Description = t.Value; break;
                                case "PluralDescription": lt.PluralDescription = t.Value; break;
                                case "Gender": lt.Gender = t.Value.FirstOrDefault(); break;
                                case "Member": lt.Members[t.Member] = t.Value; break;
                                default: throw new InvalidOperationException("Unexpected kind {0}".Formato(t.Kind)); break;
                            }
                        }
                    });

                    la.ExportXml();
                });

            return RedirectToAction("View", new { assembky = assembly });
        }

        public ActionResult Sync(string p1, string p2)
        {
            return null;
        }


    }

    public class TranslationFile
    {
        public Assembly Assembly;
        public CultureInfo CultureInfo;
        public string FileName;

        public TranslationFileStatus Status()
        {
            if (!System.IO.File.Exists(FileName))
                return TranslationFileStatus.NotCreated;

            if (System.IO.File.GetLastWriteTime(Assembly.Location) > System.IO.File.GetLastWriteTime(FileName))
                return TranslationFileStatus.Outdated;

            return TranslationFileStatus.InSync;
        }
    }


    public enum TranslationFileStatus
    {
        NotCreated,
        Outdated,
        InSync
    }
}
