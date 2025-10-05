using System.Text.RegularExpressions;

namespace Signum.CodeGeneration;

public static class CodeGenerator
{
    public static EntityCodeGenerator Entities = new EntityCodeGenerator();
    public static LogicCodeGenerator Logic = new LogicCodeGenerator();
    public static ReactCodeGenerator React = new ReactCodeGenerator();

    public static void GenerateCodeConsole()
    {
        while (true)
        {
            var action = new ConsoleSwitch<string, Action>("What do you want to generate today?")
            {
                {"E", Entities.GenerateEntitiesFromDatabaseTables, "Entities (from Database tables)"},
                {"L", Logic.GenerateLogicFromEntities, "Logic (from entites)"},
                {"R", React.GenerateReactFromEntities, "React (from entites)"},
            }.Choose();

            if (action == null)
                return;

            action();

            if (action == Entities.GenerateEntitiesFromDatabaseTables)
                return;
        }
    }

    internal static void GetSolutionInfo(out string solutionFolder, out string solutionName)
    {
        var m = Regex.Match(Environment.CurrentDirectory, @"(?<solutionFolder>.*)\\(?<solutionName>.*).Terminal\\bin\\(Debug|Release)", RegexOptions.ExplicitCapture);

        if (!m.Success)
            throw new InvalidOperationException("Unable to GetSolutionInfo from non-standart path " + Environment.CurrentDirectory + ". Override GetSolutionInfo");

        solutionFolder = m.Groups["solutionFolder"].Value;
        solutionName = m.Groups["solutionName"].Value;
    }

    public static IEnumerable<Module> GetModules(Dictionary<Type, bool> types, string solutionName)
    {
        while (true)
        {
            var typesToShow = types.Keys.OrderBy(a => types[a]).ThenBy(a => a.FullName).ToList();

            var selected = new ConsoleSwitch<int, Type>("Chose types for a new Logic module:")
                .Load(typesToShow, t => (types[t] ? "-" : " ") + t.FullName)
                .ChooseMultiple();

            if (selected.IsNullOrEmpty())
                yield break;

            string moduleName = GetDefaultModuleName(selected, solutionName)!;
            SafeConsole.WriteColor(ConsoleColor.Gray, $"Module name? ([Enter] for '{moduleName}'):");

            moduleName = Console.ReadLine().DefaultText(moduleName!);

            yield return new Module(moduleName, selected.ToList());

            types.SetRange(selected, a => a, a => true);
        }

    }

    public static string? GetDefaultModuleName(Type[] selected, string solutionName)
    {
        StringDistance sd = new StringDistance();

        string? name = selected.Select(a => (a.Namespace ?? "").RemovePrefix(solutionName)).Distinct().Only();

        if (name == null)
            return null;

        name = name.Replace(".", "");

        return name;
    }
}

public class Module
{
    public string ModuleName;
    public List<Type> Types;

    public Module(string moduleName, List<Type> types)
    {
        ModuleName = moduleName;
        Types = types;
    }
}
