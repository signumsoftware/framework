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
using Signum.Utilities.ExpressionTrees;
using System.Collections.Concurrent;

namespace Signum.Engine.Translation
{
    public static class TranslationLogic
    {
        public static TranslationOccurrences Occurrences = new TranslationOccurrences();


        static Expression<Func<IUserEntity, TranslatorUserEntity>> TranslatorUserExpression =
             user => Database.Query<TranslatorUserEntity>().SingleOrDefault(a => a.User.RefersTo(user));
        [ExpressionField]
        public static TranslatorUserEntity TranslatorUser(this IUserEntity entity)
        {
            return TranslatorUserExpression.Evaluate(entity);
        }


        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool notLocalizedMemeberRegister)
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

                if (notLocalizedMemeberRegister)
                    DescriptionManager.NotLocalizedMemeber += DescriptionManager_NotLocalizedMemeber;


            }
        }

        private static void DescriptionManager_NotLocalizedMemeber(CultureInfo ci, Type type, MemberInfo mi)
        {
            if (UserEntity.Current == null)
                return;

            var typeUsed = mi != null ? mi.ReflectedType : type;


            var dict = GetRoleNotLocalizedMemebers(UserEntity.Current.Role);
            var typeMiLongDit = dict.GetTypeMiLongDit(ci, typeUsed);

            if (mi == null)
                typeMiLongDit.AddOrUpdate(type, new TypeOccurrentes { Ocurrences = 1 }, (id, e) => { e.Ocurrences += 1; return e; });
            else {
                var miLongDit = typeMiLongDit.GetMiLongDit(typeUsed);
                miLongDit.DictMi.AddOrUpdate(mi, 1, (id, count) => count + 1);
            }
        }

        public static ConcurrentDictionary<CultureInfo, ConcurrentDictionary<Type, TypeOccurrentes>> GetRoleNotLocalizedMemebers(Lite<RoleEntity> role)
        {
            return Occurrences.LocalizableTypeUsedNotLocalized.GetOrCreate(role, new ConcurrentDictionary<CultureInfo, ConcurrentDictionary<Type, TypeOccurrentes>>());
        }

        public static ConcurrentDictionary<Type, TypeOccurrentes> GetTypeMiLongDit(this ConcurrentDictionary<CultureInfo, ConcurrentDictionary<Type, TypeOccurrentes>> dict, CultureInfo ci, Type type)
        {
            return dict.GetOrCreate(ci, new ConcurrentDictionary<Type, TypeOccurrentes>());
        }


        public static TypeOccurrentes GetMiLongDit(this ConcurrentDictionary<Type, TypeOccurrentes> dict, Type type)
        {
            return dict.GetOrCreate(type, new TypeOccurrentes());
        }



        public static long GetCountNotLocalizedMemebers(Lite<RoleEntity> role, CultureInfo ci, MemberInfo mi)
        {
            var dict = GetRoleNotLocalizedMemebers(role);
            var typeMiLongDit = dict.GetTypeMiLongDit(ci, mi.ReflectedType);


            var miLongDit = typeMiLongDit.GetMiLongDit(mi.ReflectedType);

            return miLongDit.DictMi.GetOrCreate(mi, 0);

        }

        public static long GetCountNotLocalizedMemebers(Lite<RoleEntity> role, CultureInfo ci, Type type)
        {

            var dict = GetRoleNotLocalizedMemebers(role);
            var typeMiLongDit = dict.GetTypeMiLongDit(ci, type);

          
            var miLongDit = typeMiLongDit.GetMiLongDit(type);

            return miLongDit.Ocurrences + miLongDit.DictMi.Values.Sum(e => e);

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

            Dictionary<string, string> memory = new Dictionary<string, string>();


            foreach (var fileName in Directory.EnumerateFiles(directoryName, "{0}.*.xml".FormatWith(assemblyName)))
            {
                var doc = XDocument.Load(fileName);

                HashSet<string> oldNames = doc.Element("Translations").Elements("Type").Select(t => t.Attribute("Name").Value).ToHashSet();

                Dictionary<string, string> replacements = AskForReplacementsWithMemory(newNames.ToHashSet(), oldNames.ToHashSet(), memory, replacementKey: Path.GetFileNameWithoutExtension(fileName)); //cloning

                var culture = fileName.After(assemblyName + ".").Before(".xml");

                var locAssem = LocalizedAssembly.FromXml(assembly, CultureInfo.GetCultureInfo(culture), doc, replacements?.Inverse());

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
            if (answers != null)
                toDelete = toDelete.Except(answers.Keys);

            memory.SetRange(toDelete.Select(n => KVP.Create(n, (string)null)));

            return result;
        }

    }

    public class TranslationOccurrences
    {
        public ConcurrentDictionary<Lite<RoleEntity>, ConcurrentDictionary<CultureInfo, ConcurrentDictionary<Type, TypeOccurrentes>>> LocalizableTypeUsedNotLocalized =
           new ConcurrentDictionary<Lite<RoleEntity>, ConcurrentDictionary<CultureInfo, ConcurrentDictionary<Type, TypeOccurrentes>>>();

    }

    public class TypeOccurrentes
    {
        public long Ocurrences;
        public ConcurrentDictionary<MemberInfo, long> DictMi = new ConcurrentDictionary<MemberInfo, long>();
    }
}