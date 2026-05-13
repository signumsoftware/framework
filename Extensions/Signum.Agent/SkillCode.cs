using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using Signum.API;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Signum.Agent;

public abstract class SkillCode
{
    public SkillCode()
    {
        if (SkillCodeLogic.IsAutoRegister)
            SkillCodeLogic.Register(this.GetType());
        else
        {
            if (!SkillCodeLogic.RegisteredCodes.ContainsKey(this.GetType().Name))
                throw new InvalidOperationException($"Type '{this.GetType().Name}' must be registered in SkillCodeLogic.Register<{this.GetType().TypeName()}>()");
        }
    }

    public string Name => this.GetType().Name;

    public Lite<SkillCustomizationEntity>? Customization { get; internal set; }

    public string ShortDescription { get; set; } = "";
    public Func<bool> IsAllowed { get; set; } = () => true;
    public Dictionary<string, Func<object?, string>>? Replacements;

    public static string SkillsDirectory = Path.Combine(
        Path.GetDirectoryName(typeof(SkillCode).Assembly.Location)!, "Skills");

    string? originalInstructions;
    public string OriginalInstructions
    {
        get { return originalInstructions ??= File.ReadAllText(Path.Combine(SkillsDirectory, this.GetType().Name.Before("Skill") + ".md")); }
        set { originalInstructions = value; }
    }
    public bool IsDefault()
    {
        if (SubSkills.Count > 0) return false;

        var defaultCode = (SkillCode)Activator.CreateInstance(GetType())!;
        if (ShortDescription != defaultCode.ShortDescription) return false;
        if (OriginalInstructions != defaultCode.OriginalInstructions) return false;

        foreach (var pi in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = pi.GetCustomAttribute<SkillPropertyAttribute>();
            if (attr == null) continue;
            var currentStr = attr.ConvertValueToString(pi.GetValue(this), pi.PropertyType);
            var defaultStr = attr.ConvertValueToString(pi.GetValue(defaultCode), pi.PropertyType);
            if (currentStr != defaultStr) return false;
        }

        return true;
    }

    // Populated from DB at resolve time, or from code when building a default tree for a factory.
    public List<(SkillCode Code, SkillActivation Activation)> SubSkills { get; } = new();

    public SkillCode WithSubSkill(SkillActivation activation, SkillCode sub)
    {
        SubSkills.Add((sub, activation));
        return this;
    }

    public string GetInstruction(object? context)
    {
        var text = OriginalInstructions;
        if (!Replacements.IsNullOrEmpty())
            text = text.Replace(Replacements.SelectDictionary(k => k, v => v(context)));

        if (SubSkills.Any())
        {
            var sb = new StringBuilder(text);
            foreach (var (sub, activation) in SubSkills)
            {
                sb.AppendLineLF("# Skill " + sub.Name);
                sb.AppendLineLF("**Summary**: " + sub.ShortDescription);
                sb.AppendLineLF();
                if (activation == SkillActivation.Eager)
                    sb.AppendLineLF(sub.GetInstruction(null));
                else
                    sb.AppendLineLF("Use the tool 'describe' to get more information about this skill and discover additional tools.");
            }
            return sb.ToString();
        }

        return text;
    }

    public void ApplyPropertyOverrides(SkillCustomizationEntity entity)
    {
        foreach (var po in entity.Properties)
        {
            var pi = this.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.Name == po.PropertyName
                    && p.GetCustomAttribute<SkillPropertyAttribute>() != null);

            if (pi == null) continue;

            var attr = pi.GetCustomAttribute<SkillPropertyAttribute>()!;
            var value = attr.ConvertFromString(po.Value, pi.PropertyType);
            pi.SetValue(this, value);
        }
    }

    public SkillCode? FindSkill(string name)
    {
        if (this.Name == name) return this;
        foreach (var (sub, _) in SubSkills)
        {
            var found = sub.FindSkill(name);
            if (found != null) return found;
        }
        return null;
    }

    public AITool? FindTool(string toolName)
    {
        var tool = GetTools().FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.InvariantCultureIgnoreCase));
        if (tool != null) return tool;
        foreach (var (sub, _) in SubSkills)
        {
            var found = sub.FindTool(toolName);
            if (found != null) return found;
        }
        return null;
    }

    public IEnumerable<SkillCode> GetSkillsRecursive()
    {
        yield return this;
        foreach (var (sub, _) in SubSkills)
            foreach (var s in sub.GetSkillsRecursive())
                yield return s;
    }

    public IEnumerable<SkillCode> GetEagerSkillsRecursive()
    {
        yield return this;
        foreach (var (sub, activation) in SubSkills)
            if (activation == SkillActivation.Eager)
                foreach (var s in sub.GetEagerSkillsRecursive())
                    yield return s;
    }

    public IEnumerable<AITool> GetToolsRecursive()
    {
        var list = GetTools().ToList();
        foreach (var (sub, activation) in SubSkills)
            if (activation == SkillActivation.Eager)
                list.AddRange(sub.GetToolsRecursive());
        return list;
    }

    IEnumerable<AITool>? cachedTools;
    internal IEnumerable<AITool> GetTools()
    {
        return (cachedTools ??= this.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null)
            .Select(m =>
            {
                Type delType = Expression.GetDelegateType(
                    m.GetParameters().Select(a => a.ParameterType).And(m.ReturnType).ToArray());
                Delegate del = m.IsStatic
                    ? Delegate.CreateDelegate(delType, m)
                    : Delegate.CreateDelegate(delType, this, m);
                string? description = m.GetCustomAttribute<DescriptionAttribute>()?.Description;
                return (AITool)AIFunctionFactory.Create(del, m.Name, description, GetJsonSerializerOptions());
            })
            .ToList());
    }

    internal IEnumerable<McpServerTool> GetMcpServerTools() =>
        GetTools().Select(t => McpServerTool.Create((AIFunction)t, new McpServerToolCreateOptions
        {
            SerializerOptions = GetJsonSerializerOptions(),
        }));

    static JsonSerializerOptions JsonSerializationOptions = new JsonSerializerOptions
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    }.AddSignumJsonConverters();

    public virtual JsonSerializerOptions GetJsonSerializerOptions() => JsonSerializationOptions;
}
