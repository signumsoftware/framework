using System.Globalization;
using Signum.Translation;
using System.Xml.Linq;
using System.IO;
using System.Collections.Concurrent;
using Signum.Authorization;
using Signum.Basics;
using Signum.Authorization.Rules;
using Signum.Engine.Sync;
using Signum.API;
using Signum.Translation.Translators;
using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using System.Text.RegularExpressions;

namespace Signum.Translation;

public static class TranslationLogic
{
    public static ConcurrentDictionary<Lite<RoleEntity>, ConcurrentDictionary<CultureInfo, ConcurrentDictionary<Type, TypeOccurrentes>>> NonLocalized =
     new ConcurrentDictionary<Lite<RoleEntity>, ConcurrentDictionary<CultureInfo, ConcurrentDictionary<Type, TypeOccurrentes>>>();

    public static Func<System.IO.FileInfo, string, string, string, string> GetTargetDirectory = GetTargetDirectoryDefault;
    public static string GetTargetDirectoryDefault(System.IO.FileInfo fi, string cleanFileName, string appName, string rootDir)
    {
        if (cleanFileName == appName)
            return $@"{rootDir}\{appName}\Translations";

        if (cleanFileName == "Signum.Utilities")
            return $@"{rootDir}\Framework\Signum.Utilities\Translations";

        if (cleanFileName == "Signum")
            return $@"{rootDir}\Framework\Signum\Translations";

        if(cleanFileName.StartsWith("Signum"))
            return $@"{rootDir}\Framework\Extensions\{cleanFileName}\Translations";

        throw new InvalidOperationException("Unexpected file with name " + fi.Name);
    }

    public static ITranslator[] Translators;

    public static void Start(SchemaBuilder sb, bool countLocalizationHits, params ITranslator[] translators)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            Translators = translators;

            CultureInfoLogic.AssertStarted(sb);
            
            PermissionLogic.RegisterTypes(typeof(TranslationPermission));
            
            if (countLocalizationHits)
                DescriptionManager.NotLocalizedMember += DescriptionManager_NotLocalizedMemeber;

            if (sb.WebServerBuilder != null)
                TranslationServer.Start(sb.WebServerBuilder);
        }
    }

    private static void DescriptionManager_NotLocalizedMemeber(CultureInfo ci, MemberInfo mi)
    {
        if (UserEntity.Current == null)
            return;

        var pi = mi as PropertyInfo;
        var type = pi?.ReflectedType ?? mi as Type;

        var typeOccurrences = NonLocalized.GetOrAdd(RoleEntity.Current).GetOrAdd(ci).GetOrAdd(type!);

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
        var cultures = CultureInfoLogic.ApplicationCultures(isNeutral: null);

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


        foreach (var path in Directory.EnumerateFiles(directoryName, "{0}.*.xml".FormatWith(assemblyName)))
        {
            var fileName = Path.GetFileNameWithoutExtension(path);

            var culture = fileName.After(assemblyName + ".");

            if (culture.Contains('.'))
                continue;

            var doc = XDocument.Load(path);

            HashSet<string> oldNames = new HashSet<string>(); ; // doc.Element("Translations")!.Elements("Type").Where(t => !LocalizedType.IsEmpty(t)).Select(t => t.Attribute("Name")!.Value).ToHashSet();

            Dictionary<string, string> replacements = AskForReplacementsWithMemory(newNames.ToHashSet(), oldNames.ToHashSet(), memory, replacementKey: Path.GetFileNameWithoutExtension(path)!); //cloning

            var locAssem = LocalizedAssembly.FromXml(assembly, CultureInfo.GetCultureInfo(culture), doc, replacements?.Inverse());

            var xml = locAssem.ToXml();
            if (xml == null)
                File.Delete(path);
            else
                xml.Save(path);
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

        var rootDir = currentDirectory.Before(@".Terminal\bin");
        var appName = rootDir.AfterLast(@"\");
        rootDir = rootDir.BeforeLast(@"\");

        var parentDir = $@"{rootDir}\{appName}.Server\bin\";

        var reactDirs = new DirectoryInfo(parentDir).GetDirectories("Translations", SearchOption.AllDirectories).ToList();

        if(reactDirs.Count == 0)
        {
            SafeConsole.WriteLineColor(ConsoleColor.DarkRed, "No Translations directory found in: " + parentDir);
            return;
        }

        var reactDir = reactDirs.Only() ??
            reactDirs.ChooseConsole(
                message: $"More than one 'Translations' folder found in '{parentDir}'",
                getString: d => $"{d.FullName} ({d.GetFiles("*.xml").Max(a => (DateTime?)a.LastWriteTime)?.ToAgoString() ?? "-No Files-"})");

        if (reactDir == null)
            return;

        foreach (var fi in reactDir.GetFiles("*.xml"))
        {
            var cleanFileName = Regex.Replace(fi.Name, @"\.(?<culture>\w+)\.xml$", "");

            var targetDirectory = GetTargetDirectory(fi, cleanFileName, appName, rootDir);

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
