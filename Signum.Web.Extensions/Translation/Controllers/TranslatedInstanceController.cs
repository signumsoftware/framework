using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Translation;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Translation;
using Signum.Utilities;

namespace Signum.Web.Translation.Controllers
{
    public class TranslatedInstanceController : Controller
    {
        public ActionResult Index()
        {
            var cultures = TranslationLogic.CurrentCultureInfos("en");

            var list = TranslatedInstanceLogic.TranslationInstancesStatus();

            return base.View(TranslationClient.ViewPrefix.Formato("IndexInstance"), list.AgGroupToDictionary(a => a.Type, gr => gr.ToDictionary(a => a.CultureInfo)));
        }

        public new ActionResult View(string type, string culture)
        {
            Type t = TypeLogic.GetType(type);

            Dictionary<LocalizedInstanceKey, string> master = TranslatedInstanceLogic.FromEntities(t);

            Dictionary<CultureInfo, Dictionary<LocalizedInstanceKey, TranslatedInstanceDN>> support = TranslatedInstanceLogic.TranslationsForType(t, culture: null);

            var c =  culture == null ? null : CultureInfo.GetCultureInfo(culture);

            ViewBag.Type = t;
            ViewBag.Master = master;
            ViewBag.Culture = c;

            return base.View(TranslationClient.ViewPrefix.Formato("ViewInstance"), support);
        }

        [HttpPost]
        public ActionResult View(string type, string culture, string bla)
        {
            Type t = TypeLogic.GetType(type);

            Dictionary<LocalizedInstanceKey, string> master = TranslatedInstanceLogic.FromEntities(t);

            Dictionary<Tuple<CultureInfo, LocalizedInstanceKey>, TranslationRecord> should = GetTranslationRecords(t).Where(a => a.Value.HasText())
                .ToDictionary(a => Tuple.Create(a.Culture, new LocalizedInstanceKey(a.Route, a.Instance)));

            var c = culture == null ? null : CultureInfo.GetCultureInfo(culture);

            Dictionary<Tuple<CultureInfo, LocalizedInstanceKey>, TranslatedInstanceDN> current = 
                (from ci in TranslatedInstanceLogic.TranslationsForType(t, c)
                from key in ci.Value
                select KVP.Create(Tuple.Create(ci.Key, key.Key), key.Value)).ToDictionary();

            using (Transaction tr = new Transaction())
            {
                Dictionary<PropertyRoute, PropertyRouteDN> routes = should.Keys.Select(a => a.Item2.Route).Distinct().ToDictionary(a => a, a => a.ToPropertyRouteDN());

                Synchronizer.Synchronize(
                    should,
                    current,
                    (k, n) =>new TranslatedInstanceDN
                    {
                        Culture = n.Culture.ToCultureInfoDN(),
                        PropertyRoute = routes.GetOrThrow(n.Route),
                        Instance = n.Instance,
                        OriginalText = master[k.Item2],
                        TranslatedText = n.Value,
                    }.Save(),
                    (k, o) => o.Delete(),
                    (k, n, o) =>
                    {
                        if (o.TranslatedText != n.Value || o.OriginalText != master[k.Item2])
                        {
                            var r = o.ToLite().Retrieve();
                            r.OriginalText = master[k.Item2];
                            r.TranslatedText = n.Value;
                            r.Save();
                        }
                    });

                tr.Commit();
            }

            return RedirectToAction("View", new { type = type, culture = culture });
        }

        static Regex regex = new Regex(@"^(?<lang>[^#]+)#(?<instance>[^#]+)#(?<route>[^#]+)$");

        private List<TranslationRecord> GetTranslationRecords(Type type)
        {
            var list = (from k in Request.Form.AllKeys
                        let m = regex.Match(k)
                        where m.Success
                        select new TranslationRecord
                        {
                            Culture = CultureInfo.GetCultureInfo(m.Groups["lang"].Value),
                            Instance = Lite.Parse(m.Groups["instance"].Value),
                            Route = PropertyRoute.Parse(type, m.Groups["route"].Value),
                            Value = Request.Form[k].DefaultText(null),
                        }).ToList();
            return list;
        }

        class TranslationRecord
        {
            public CultureInfo Culture;
            public Lite<IdentifiableEntity> Instance;
            public PropertyRoute Route;
            public string Value;

            public override string ToString()
            {
                return "{0} {1} {2} -> {3}".Formato(Culture, Instance, Route, Value);
            }
        }

        public ActionResult Sync(string type, string culture)
        {
            Type t = TypeLogic.GetType(type);

            var c = CultureInfo.GetCultureInfo(culture);

            int totalInstances; 
            var changes = TranslatedInstanceSynchronizer.GetTypeInstanceChanges(TranslationClient.Translator, t, c, out totalInstances);

            ViewBag.TotalInstances = totalInstances; 
            ViewBag.Culture = c;
            return base.View(TranslationClient.ViewPrefix.Formato("SyncInstance"), changes);
        }

        [HttpPost]
        public ActionResult Sync(string type, string culture, string bla)
        {
            Type t = TypeLogic.GetType(type);

            var c = CultureInfo.GetCultureInfo(culture);

            List<TranslationRecord> records = GetTranslationRecords(t);

            Dictionary<LocalizedInstanceKey, string> master = TranslatedInstanceLogic.FromEntities(t);

            var current = TranslatedInstanceLogic.TranslationsForType(t, c).SingleOrDefault().Value; 

            Dictionary<PropertyRoute, PropertyRouteDN> routes = records.Select(a => a.Route).Distinct().ToDictionary(a => a, a => a.ToPropertyRouteDN());
            using (Transaction tr = new Transaction())
            {
                records.Where(r => r.Value.HasText()).Select(r =>
                {
                    var key = new LocalizedInstanceKey(r.Route, r.Instance);

                    TranslatedInstanceDN entity = current.TryGetC(key);

                    if (entity != null)
                    {
                        entity = entity.ToLite().Retrieve();
                        entity.OriginalText = master[key];
                        entity.TranslatedText = r.Value;
                        return entity;
                    }
                    else
                    {
                        return new TranslatedInstanceDN
                        {
                            Culture = r.Culture.ToCultureInfoDN(),
                            PropertyRoute = routes.GetOrThrow(r.Route),
                            Instance = r.Instance,
                            OriginalText = master[key],
                            TranslatedText = r.Value,
                        };
                    }

                }).SaveList();

                TranslatedInstanceLogic.CleanTranslations(t);

                tr.Commit();
            }

            return RedirectToAction("Index");
        }
    }
}
