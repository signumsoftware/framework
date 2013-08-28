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
            var cultures = TranslationLogic.CurrentCultureInfos("en");

            var dic = AssembliesToLocalize().ToDictionary(a => a,
                a => cultures.Select(ci => new TranslationFile
                {
                    Assembly = a,
                    CultureInfo = ci,
                    IsDefault = ci.Name == a.SingleAttribute<DefaultAssemblyCultureAttribute>().DefaultCulture,
                    FileName = LocalizedAssembly.TranslationFileName(a, ci)
                }).ToDictionary(tf => tf.CultureInfo));

            return base.View(TranslationClient.ViewPrefix.Formato("Index"), dic);
        }

        public new ActionResult View(string assembly, string culture)
        {
            Assembly ass = AssembliesToLocalize().Where(a => a.GetName().Name == assembly).SingleEx(() => "Assembly {0}".Formato(assembly));

            CultureInfo defaultCulture = CultureInfo.GetCultureInfo(ass.SingleAttribute<DefaultAssemblyCultureAttribute>().DefaultCulture);

            Dictionary<CultureInfo, LocalizedAssembly> reference = (from ci in TranslationLogic.CurrentCultureInfos(defaultCulture.Name)
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

            List<TranslationRecord> list = GetTranslationRecords();

            if (culture.HasText())
            {
                LocalizedAssembly locAssembly = LocalizedAssembly.ImportXml(currentAssembly, CultureInfo.GetCultureInfo(culture));

                list.GroupToDictionary(a => a.Type).JoinDictionaryForeach(locAssembly.Types.Values.ToDictionary(a => a.Type.Name), (tn, tuples, lt) =>
                {
                    foreach (var t in tuples)
                        t.Apply(lt);
                });

                locAssembly.ExportXml();
            }
            else
            {
                Dictionary<string, LocalizedAssembly> locAssemblies = TranslationLogic.CurrentCultureInfos("en").ToDictionary(ci => ci.Name, ci => LocalizedAssembly.ImportXml(currentAssembly, ci));

                Dictionary<string, List<TranslationRecord>> groups = list.GroupToDictionary(a => a.Lang);

                list.GroupToDictionary(a => a.Lang).JoinDictionaryForeach(locAssemblies, (cn, recs, la) =>
                {
                    recs.GroupToDictionary(a => a.Type).JoinDictionaryForeach(la.Types.Values.ToDictionary(a => a.Type.Name), (tn, tuples, lt) =>
                    {
                        foreach (var t in tuples)
                            t.Apply(lt);
                    });

                    la.ExportXml();
                });
            }
            return RedirectToAction("View", new { assembly = assembly, culture = culture });
        }

        static Regex regex = new Regex(@"^(?<type>[_\w][_\w\d]*(`\d)?)\.(?<lang>[\w_\-]+)\.(?<kind>\w+)(\.(?<member>[_\w][_\w\d]*))?$");

        private List<TranslationRecord> GetTranslationRecords()
        {
            var list = (from k in Request.Form.AllKeys
                        let m = regex.Match(k)
                        where m.Success
                        select new TranslationRecord
                        {
                            Type = m.Groups["type"].Value,
                            Lang = m.Groups["lang"].Value,
                            Kind = m.Groups["kind"].Value.ToEnum<TranslationRecordKind>(),
                            Member = m.Groups["member"].Value,
                            Value = Request.Form[k].DefaultText(null),
                        }).ToList();
            return list;
        }

        public JsonResult PluralAndGender()
        {
            string name = Request.Form["name"];

            CultureInfo ci = CultureInfo.GetCultureInfo(regex.Match(name).Groups["lang"].Value);

            string text = Request.Form["text"];

            return Json(new
            {
                gender = NaturalLanguageTools.GetGender(text, ci),
                plural = NaturalLanguageTools.Pluralize(text, ci)
            });
        }


        class TranslationRecord
        {
            public string Type;
            public string Lang;
            public TranslationRecordKind Kind;
            public string Member;
            public string Value;

            internal void Apply(LocalizedType lt)
            {
                switch (Kind)
                {
                    case TranslationRecordKind.Description: lt.Description = Value; break;
                    case TranslationRecordKind.PluralDescription: lt.PluralDescription = Value; break;
                    case TranslationRecordKind.Gender: lt.Gender = Value != null ? (char?)Value[0] : null; break;
                    case TranslationRecordKind.Member: lt.Members[Member] = Value; break;
                    default: throw new InvalidOperationException("Unexpected kind {0}".Formato(Kind));
                }
            }
        }

        public enum TranslationRecordKind
        {
            Description,
            PluralDescription,
            Gender,
            Member,
        }

        public ActionResult Sync(string assembly, string culture)
        {
            Assembly ass = AssembliesToLocalize().Where(a => a.GetName().Name == assembly).SingleEx(() => "Assembly {0}".Formato(assembly));
            var targetCI = CultureInfo.GetCultureInfo(culture);

            CultureInfo defaultCulture = CultureInfo.GetCultureInfo(ass.SingleAttribute<DefaultAssemblyCultureAttribute>().DefaultCulture);

            Dictionary<CultureInfo, LocalizedAssembly> reference = (from ci in TranslationLogic.CurrentCultureInfos(defaultCulture.Name)
                                                                    let la = DescriptionManager.GetLocalizedAssembly(ass, ci)
                                                                    where la != null || ci == defaultCulture
                                                                    select KVP.Create(ci, la ?? LocalizedAssembly.ImportXml(ass, ci))).ToDictionary();
            var master = reference.Extract(defaultCulture);
            
            var target = reference.Extract(targetCI);
            DictionaryByTypeName(target); //To avoid finding duplicated types on save
            var changes = TranslationSynchronizer.GetAssemblyChanges(TranslationClient.Translator, target, master, reference.Values.ToList());

            ViewBag.Culture = targetCI;
            return base.View(TranslationClient.ViewPrefix.Formato("Sync"), changes);
        }

        [HttpPost]
        public ActionResult Sync(string assembly, string culture, string bla)
        {
            Assembly currentAssembly = AssembliesToLocalize().Where(a => a.GetName().Name == assembly).SingleEx(() => "Assembly {0}".Formato(assembly));
            
            LocalizedAssembly locAssembly = LocalizedAssembly.ImportXml(currentAssembly, CultureInfo.GetCultureInfo(culture));

            List<TranslationRecord> list = GetTranslationRecords();

            list.GroupToDictionary(a => a.Type).JoinDictionaryForeach(DictionaryByTypeName(locAssembly), (tn, tuples, lt) =>
            {
                foreach (var t in tuples)
                    t.Apply(lt);
            });

            locAssembly.ExportXml();

            return RedirectToAction("Index");
        }

        private static Dictionary<string, LocalizedType> DictionaryByTypeName(LocalizedAssembly locAssembly)
        {
            return locAssembly.Types.Values.ToDictionary(a => a.Type.Name, "LocalizedTypes");
        }
    }

    public class TranslationFile
    {
        public Assembly Assembly;
        public CultureInfo CultureInfo;
        public string FileName;
        public bool IsDefault; 

        public TranslatedSummaryState Status()
        {
            if (!System.IO.File.Exists(FileName))
                return TranslatedSummaryState.None;

            if (System.IO.File.GetLastWriteTime(Assembly.Location) > System.IO.File.GetLastWriteTime(FileName))
                return TranslatedSummaryState.Pending;

            return TranslatedSummaryState.Completed;
        }
    }


    public enum TranslationFileStatus
    {
        NotCreated,
        Outdated,
        InSync
    }
}
