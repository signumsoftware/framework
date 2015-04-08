using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Translation;
using Signum.Utilities;
using System.Xml.Linq;
using System.IO;

namespace Signum.Engine.Translation
{
    public static class TranslationLogic
    {
        static Expression<Func<IUserEntity, TranslatorUserEntity>> TranslatorUserExpression =
             user => Database.Query<TranslatorUserEntity>().SingleOrDefault(a => a.User.RefersTo(user));
        public static TranslatorUserEntity TranslatorUser(this IUserEntity entity)
        {
            return TranslatorUserExpression.Evaluate(entity);
        }

  
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoLogic.AssertStarted(sb);

                sb.Include<TranslatorUserEntity>();

                dqm.RegisterQuery(typeof(TranslatorUserEntity), () =>
                    from e in Database.Query<TranslatorUserEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.User,
                        Cultures = e.Cultures.Count,
                    });


                PermissionAuthLogic.RegisterTypes(typeof(TranslationPermission));

                dqm.RegisterExpression((IUserEntity e) => e.TranslatorUser(), () => typeof(TranslatorUserEntity).NiceName());

                new Graph<TranslatorUserEntity>.Execute(TranslatorUserOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { }
                }.Register();

                new Graph<TranslatorUserEntity>.Delete(TranslatorUserOperation.Delete)
                {
                    Delete = (e, _) => { e.Delete(); }
                }.Register();
            }
        }

        public static List<CultureInfo> CurrentCultureInfos(CultureInfo defaultCulture)
        {
            var cultures = CultureInfoLogic.ApplicationCultures;

            if (Schema.Current.Tables.ContainsKey(typeof(TranslatorUserEntity)))
            {
                TranslatorUserEntity tr = UserEntity.Current.TranslatorUser();

                if (tr != null)
                    cultures = cultures.Where(ci => ci.Name == defaultCulture.Name || tr.Cultures.Any(tc => tc.Culture.ToCultureInfo() == ci));
            }

            return cultures.OrderByDescending(a => a.Name == defaultCulture.Name).ThenBy(a => a.Name).ToList();
        }

        public static void SynchronizeTypes(Assembly assembly, string directoryName)
        {
            string assemblyName = assembly.GetName().Name;

            HashSet<string> newNames = (from t in assembly.GetTypes()
                                        let opts = LocalizedAssembly.GetDescriptionOptions(t)
                                        where opts != DescriptionOptions.None
                                        select t.Name).ToHashSet();

            Dictionary<string, string> memory = new Dictionary<string,string>();


            foreach (var fileName in Directory.EnumerateFiles(directoryName, "{0}.*.xml".FormatWith(assemblyName)))
            {
                var doc = XDocument.Load(fileName);

                HashSet<string> oldNames = doc.Element("Translations").Elements("Type").Select(t => t.Attribute("Name").Value).ToHashSet();

                Dictionary<string, string> replacements = AskForReplacementsWithMemory(newNames.ToHashSet(), oldNames.ToHashSet(), memory, replacementKey: Path.GetFileNameWithoutExtension(fileName)); //cloning

                var culture = fileName.After(assemblyName + ".").Before(".xml");

                var locAssem = LocalizedAssembly.FromXml(assembly, CultureInfo.GetCultureInfo(culture), doc, replacements.Try(r => r.Inverse()));

                locAssem.ToXml().Save(fileName);
            }
        }

        private static Dictionary<string, string> AskForReplacementsWithMemory(HashSet<string> newNames, HashSet<string> oldNames, Dictionary<string, string> memory, string replacementKey)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (var kvp in memory)
            {
                if (oldNames.Contains(kvp.Key) && kvp.Value == null)
                {
                    oldNames.Remove(kvp.Key);
                }
                else if (oldNames.Contains(kvp.Key) && newNames.Contains(kvp.Value))
                {
                    oldNames.Remove(kvp.Key);
                    newNames.Remove(kvp.Value);
                    result.Add(kvp.Key, kvp.Value);
                }
            }

            Replacements rep = new Replacements();

            rep.AskForReplacements(oldNames, newNames, replacementKey);

            var answers = rep.TryGetC(replacementKey);
            if (answers != null)
            {
                result.AddRange(answers);
                memory.SetRange(answers);
            }

            var toDelete = oldNames.Except(newNames);
            if(answers != null)
                toDelete = toDelete.Except(answers.Keys);

            memory.SetRange(toDelete.Select(n => KVP.Create(n, (string)null)));

            return result;
        }
    }
}
