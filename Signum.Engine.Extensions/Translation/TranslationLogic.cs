using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Translation;
using Signum.Utilities;
using System.Xml.Linq;
using System.IO;
using System.Collections.Concurrent;

namespace Signum.Engine.Translation
{
    public static class TranslationLogic
    {
        public static ConcurrentDictionary<Lite<RoleEntity>, ConcurrentDictionary<CultureInfo, ConcurrentDictionary<Type, TypeOccurrentes>>> NonLocalized =
         new ConcurrentDictionary<Lite<RoleEntity>, ConcurrentDictionary<CultureInfo, ConcurrentDictionary<Type, TypeOccurrentes>>>();



        public static void Start(SchemaBuilder sb, bool countLocalizationHits)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoLogic.AssertStarted(sb);
                
                PermissionAuthLogic.RegisterTypes(typeof(TranslationPermission));
                
                if (countLocalizationHits)
                    DescriptionManager.NotLocalizedMember += DescriptionManager_NotLocalizedMemeber;
            }
        }

        private static void DescriptionManager_NotLocalizedMemeber(CultureInfo ci, MemberInfo mi)
        {
            if (UserEntity.Current == null)
                return;

            var pi = mi as PropertyInfo;
            var type = pi?.ReflectedType ?? mi as Type;

            var typeOccurrences = NonLocalized.GetOrAdd(UserEntity.Current.Role).GetOrAdd(ci).GetOrAdd(type!);

            if (pi == null)
                typeOccurrences.Ocurrences++;
            else
                typeOccurrences.Members.AddOrUpdate(mi, 1, (id, count) => count + 1);
        }
        

        public static long GetCountNotLocalizedMemebers(Lite<RoleEntity> role, CultureInfo ci, MemberInfo mi)
        {
            return NonLocalized.GetOrAdd(role).GetOrAdd(ci).GetOrThrow(mi.ReflectedType!).Members.GetOrAdd(mi, 0);
        }

        public static long GetCountNotLocalizedMemebers(Lite<RoleEntity> role, CultureInfo ci, Type type)
        {
            return NonLocalized.GetOrAdd(role).GetOrAdd(ci).GetOrThrow(type).TotalCount;          
        }

        public static List<CultureInfo> CurrentCultureInfos(CultureInfo defaultCulture)
        {
            var cultures = CultureInfoLogic.ApplicationCultures;

            return cultures.OrderByDescending(a => a.Name == defaultCulture.Name).ThenBy(a => a.Name).ToList();
        }

        public static void SynchronizeTypes(Assembly assembly, string directoryName)
        {
            string assemblyName = assembly.GetName().Name!;

            HashSet<string> newNames = (from t in assembly.GetTypes()
                                        let opts = LocalizedAssembly.GetDescriptionOptions(t)
                                        where opts != DescriptionOptions.None
                                        select t.Name).ToHashSet();

            Dictionary<string, string?> memory = new Dictionary<string, string?>();


            foreach (var fileName in Directory.EnumerateFiles(directoryName, "{0}.*.xml".FormatWith(assemblyName)))
            {
                var doc = XDocument.Load(fileName);

                HashSet<string> oldNames = doc.Element("Translations").Elements("Type").Select(t => t.Attribute("Name").Value).ToHashSet();

                Dictionary<string, string> replacements = AskForReplacementsWithMemory(newNames.ToHashSet(), oldNames.ToHashSet(), memory, replacementKey: Path.GetFileNameWithoutExtension(fileName)!); //cloning

                var culture = fileName.After(assemblyName + ".").Before(".xml");

                var locAssem = LocalizedAssembly.FromXml(assembly, CultureInfo.GetCultureInfo(culture), doc, replacements?.Inverse());

                locAssem.ToXml().Save(fileName);
            }
        }

        private static Dictionary<string, string> AskForReplacementsWithMemory(HashSet<string> newNames, HashSet<string> oldNames, Dictionary<string, string?> memory, string replacementKey)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (var kvp in memory)
            {
                if (oldNames.Contains(kvp.Key) && kvp.Value == null)
                {
                    oldNames.Remove(kvp.Key);
                }
                else if (oldNames.Contains(kvp.Key) && newNames.Contains(kvp.Value!))
                {
                    oldNames.Remove(kvp.Key);
                    newNames.Remove(kvp.Value!);
                    result.Add(kvp.Key, kvp.Value!);
                }
            }

            Replacements rep = new Replacements();

            rep.AskForReplacements(oldNames, newNames, replacementKey);

            var answers = rep.TryGetC(replacementKey);
            if (answers != null)
            {
                result.AddRange(answers);
                memory!.SetRange(answers);
            }

            var toDelete = oldNames.Except(newNames);
            if (answers != null)
                toDelete = toDelete.Except(answers.Keys);

            memory.SetRange(toDelete.Select(n => KeyValuePair.Create(n, (string?)null)));

            return result;
        }

        public static void CopyTranslations()
        {
            var currentDirectory = Directory.GetCurrentDirectory();

            var rootDir = currentDirectory.Before(@".Load\bin");
            var appName = rootDir.AfterLast(@"\");
            rootDir = rootDir.BeforeLast(@"\");

            var reactDir = new DirectoryInfo($@"{rootDir}\{appName}.React\bin\").GetDirectories("Translations", SearchOption.AllDirectories).SingleEx();

            foreach (var fi in reactDir.GetFiles("*.xml"))
            {
                var targetDirectory =
                    fi.Name.StartsWith(appName + ".Entities") ? $@"{rootDir}\{appName}.Entities\Translations" :
                    fi.Name.StartsWith("Signum.Entities.Extensions") ? $@"{rootDir}\Extensions\Signum.Entities.Extensions\Translations" :
                    fi.Name.StartsWith("Signum.Entities") ? $@"{rootDir}\Framework\Signum.Entities\Translations" :
                    fi.Name.StartsWith("Signum.Utilities") ? $@"{rootDir}\Framework\Signum.Utilities\Translations" :
                    throw new InvalidOperationException("Unexpected file with name " + fi.Name);

                if (!Directory.Exists(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);

                var targetFilePath = Path.Combine(targetDirectory, fi.Name);
                Console.WriteLine(targetFilePath);
                File.Copy(fi.FullName, targetFilePath, overwrite: true);
            }
        }
    }


    public class TypeOccurrentes
    {
        public long Ocurrences;
        public ConcurrentDictionary<MemberInfo, long> Members = new ConcurrentDictionary<MemberInfo, long>();

        public long TotalCount => Ocurrences + Members.Values.Sum();
    }
}
