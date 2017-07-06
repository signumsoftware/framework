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
using Signum.Utilities.DataStructures;
using Signum.Entities;
using Signum.Engine;

namespace Signum.Web.Translation.Controllers
{
    [ValidateInputAttribute(false)]
    public class TranslationController : Controller
    {
        public static IEnumerable<Assembly> AssembliesToLocalize()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(a => a.HasAttribute<DefaultAssemblyCultureAttribute>());
        }



        public ActionResult Index(Lite<RoleEntity> role)
        {
            var cultures = TranslationLogic.CurrentCultureInfos(CultureInfo.GetCultureInfo("en"));

            var assemblies = AssembliesToLocalize().ToDictionary(a => a.FullName);

            var dg = DirectedGraph<Assembly>.Generate(assemblies.Values, a => a.GetReferencedAssemblies().Select(an => assemblies.TryGetC(an.FullName)).NotNull());

            var dic = dg.CompilationOrderGroups().SelectMany(gr => gr.OrderBy(a => a.FullName)).ToDictionary(a => a,
                a => cultures.Select(ci => new TranslationFile
                {
                    Assembly = a,
                    CultureInfo = ci,
                    IsDefault = ci.Name == a.GetCustomAttribute<DefaultAssemblyCultureAttribute>().DefaultCulture,
                    FileName = LocalizedAssembly.TranslationFileName(a, ci)
                }).ToDictionary(tf => tf.CultureInfo));


            if (role != null)
                ViewBag.Role = role.InDB().Select(e => e.ToLite()).SingleEx();

            return base.View(TranslationClient.ViewPrefix.FormatWith("Index"), dic);
        }



        public ActionResult LocalizableTypeUsedNotLocalized(Lite<RoleEntity> role)
        {

            if (role != null)
                ViewBag.Role = role.InDB().Select(e => e.ToLite()).SingleEx();
            return base.View(TranslationClient.ViewPrefix.FormatWith("LocalizableTypeUsedNotLocalized"), TranslationLogic.NonLocalized);
        }


        [HttpGet]
        public ActionResult View(string assembly, string culture, bool searchPressed, string filter)
        {
            Assembly ass = AssembliesToLocalize().Where(a => a.GetName().Name == assembly).SingleEx(() => "Assembly {0}".FormatWith(assembly));

            CultureInfo defaultCulture = CultureInfo.GetCultureInfo(ass.GetCustomAttribute<DefaultAssemblyCultureAttribute>().DefaultCulture);
            CultureInfo targetCulture = culture == null ? null : CultureInfo.GetCultureInfo(culture);

            Dictionary<CultureInfo, LocalizedAssembly> reference = !searchPressed ? null :
                (from ci in TranslationLogic.CurrentCultureInfos(defaultCulture)
                 let la = DescriptionManager.GetLocalizedAssembly(ass, ci)
                 where la != null || ci == defaultCulture || ci == targetCulture
                 select KVP.Create(ci, la ?? LocalizedAssembly.ImportXml(ass, ci, forceCreate: true))).ToDictionary();

            ViewBag.filter = filter;
            ViewBag.searchPressed = searchPressed;
            ViewBag.Assembly = ass;
            ViewBag.DefaultCulture = defaultCulture;
            ViewBag.Culture = targetCulture;

            return base.View(TranslationClient.ViewPrefix.FormatWith("View"), reference);
        }

        [HttpPost]
        public ActionResult SaveView(string assembly, string culture, string filter)
        {
            var currentAssembly = AssembliesToLocalize().Single(a => a.GetName().Name == assembly);

            List<TranslationRecord> list = GetTranslationRecords();

            if (culture.HasText())
            {
                LocalizedAssembly locAssembly = LocalizedAssembly.ImportXml(currentAssembly, CultureInfo.GetCultureInfo(culture), forceCreate: true);

                list.GroupToDictionary(a => a.Type).JoinDictionaryForeach(locAssembly.Types.Values.ToDictionary(a => a.Type.Name), (tn, tuples, lt) =>
                {
                    foreach (var t in tuples)
                        t.Apply(lt);
                });

                locAssembly.ExportXml();
            }
            else
            {
                Dictionary<string, LocalizedAssembly> locAssemblies = TranslationLogic.CurrentCultureInfos(CultureInfo.GetCultureInfo("en")).ToDictionary(ci => ci.Name,
                    ci => LocalizedAssembly.ImportXml(currentAssembly, ci, forceCreate: true));

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
            return RedirectToAction("View", new { assembly = assembly, culture = culture, searchPressed = true, filter = filter });
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

        public JsonNetResult PluralAndGender()
        {
            string name = Request.Form["name"];

            CultureInfo ci = CultureInfo.GetCultureInfo(regex.Match(name).Groups["lang"].Value);

            string text = Request.Form["text"];

            return this.JsonNet(new
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
                    default: throw new InvalidOperationException("Unexpected kind {0}".FormatWith(Kind));
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

        public ActionResult Sync(string assembly, string culture, bool translatedOnly, Lite<RoleEntity> role)
        {
            Assembly ass = AssembliesToLocalize().Where(a => a.GetName().Name == assembly).SingleEx(() => "Assembly {0}".FormatWith(assembly));
            CultureInfo targetCulture = CultureInfo.GetCultureInfo(culture);

            CultureInfo defaultCulture = CultureInfo.GetCultureInfo(ass.GetCustomAttribute<DefaultAssemblyCultureAttribute>().DefaultCulture);

            Dictionary<CultureInfo, LocalizedAssembly> reference = (from ci in TranslationLogic.CurrentCultureInfos(defaultCulture)
                                                                    let la = DescriptionManager.GetLocalizedAssembly(ass, ci)
                                                                    where la != null || ci == defaultCulture || ci == targetCulture
                                                                    select KVP.Create(ci, la ?? LocalizedAssembly.ImportXml(ass, ci, forceCreate: true))).ToDictionary();
            var master = reference.Extract(defaultCulture);

            var target = reference.Extract(targetCulture);
            DictionaryByTypeName(target); //To avoid finding duplicated types on save
            int totalTypes;
            var changes = TranslationSynchronizer.GetAssemblyChanges(TranslationClient.Translator, target, master, reference.Values.ToList(), role, null, out totalTypes);

            ViewBag.Role = role;
            ViewBag.TotalTypes = totalTypes;
            ViewBag.Culture = targetCulture;
            return base.View(TranslationClient.ViewPrefix.FormatWith("Sync"), changes);
        }

        [HttpPost]
        public ActionResult SaveSync(string assembly, string culture)
        {
            Assembly currentAssembly = AssembliesToLocalize().Where(a => a.GetName().Name == assembly).SingleEx(() => "Assembly {0}".FormatWith(assembly));

            LocalizedAssembly locAssembly = LocalizedAssembly.ImportXml(currentAssembly, CultureInfo.GetCultureInfo(culture), forceCreate: true);

            List<TranslationRecord> records = GetTranslationRecords();

            records.GroupToDictionary(a => a.Type).JoinDictionaryForeach(DictionaryByTypeName(locAssembly), (tn, tuples, lt) =>
            {
                foreach (var t in tuples)
                    t.Apply(lt);
            });

            locAssembly.ExportXml();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public void Feedback(string culture, string wrong, string right)
        {
            ((ITranslatorWithFeedback)TranslationClient.Translator).Feedback(culture, wrong, right);
        }

        static Dictionary<string, LocalizedType> DictionaryByTypeName(LocalizedAssembly locAssembly)
        {
            return locAssembly.Types.Values.ToDictionaryEx(a => a.Type.Name, "LocalizedTypes");
        }
    }

    public class TranslationFile
    {
        public Assembly Assembly;
        public CultureInfo CultureInfo;
        public string FileName;
        public bool IsDefault;

        public TranslatedSummaryState Status(Lite<RoleEntity> role)
        {

            if (!System.IO.File.Exists(FileName))
                return TranslatedSummaryState.None;

            var target = DescriptionManager.GetLocalizedAssembly(Assembly, CultureInfo);

            CultureInfo defaultCulture = CultureInfo.GetCultureInfo(Assembly.GetCustomAttribute<DefaultAssemblyCultureAttribute>().DefaultCulture);
            var master = DescriptionManager.GetLocalizedAssembly(Assembly, defaultCulture);

            var result = TranslationSynchronizer.GetMergeChanges(target, master, new List<LocalizedAssembly>());


            if (result.Any(r => role == null || TranslationLogic.GetCountNotLocalizedMemebers(role, CultureInfo, r.Type.Type) > 0))
                return TranslatedSummaryState.Pending;


            return TranslatedSummaryState.Completed;
        }
    }
}
