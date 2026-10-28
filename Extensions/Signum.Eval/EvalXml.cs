using System.Xml.Linq;

namespace Signum.Eval;

/// <summary>
/// The user-asset XML of a script stored in two languages (EvalEmbedded.ScriptCS / ScriptTS, and
/// WorkflowScriptEval's custom types), so a Signum application and an Altea one sharing a database exchange
/// the same files. The original element keeps the C# (Script, ActorsEval, Applicable...), so every file
/// exported before still imports; the TypeScript is a sibling with "TS" appended. A missing element keeps
/// the current value, so re-importing a file exported before a script was translated does not wipe it.
/// </summary>
public static class EvalXml
{
    /// <summary>The C# element (when there is C#), and the TypeScript one when EvalLogic.AlteaMigration is on and there is any.</summary>
    public static IEnumerable<XElement> ToXml(string name, string? cs, string? ts)
    {
        if (cs != null)
            yield return new XElement(name, new XCData(cs));

        if (EvalLogic.AlteaMigration && ts.HasText())
            yield return new XElement(name + "TS", new XCData(ts));
    }

    public static IEnumerable<XElement> ToXml<T>(string name, EvalEmbedded<T> eval) where T : class
        => ToXml(name, eval.ScriptCS, eval.ScriptTS);

    /// <summary>Whether the element carries either language of the script.</summary>
    public static bool HasScripts(XElement parent, string name)
        => parent.Element(name) != null || parent.Element(name + "TS") != null;

    /// <summary>Hands each language PRESENT in the file to its setter; a missing one is left as it is.</summary>
    public static void FromXml(XElement parent, string name, Action<string> setCS, Action<string> setTS)
    {
        var cs = parent.Element(name);
        if (cs != null)
            setCS(cs.Value);

        var ts = parent.Element(name + "TS");
        if (ts != null)
            setTS(ts.Value);
    }

    public static void FromXml<T>(XElement parent, string name, EvalEmbedded<T> eval) where T : class
        => FromXml(parent, name, cs => eval.ScriptCS = cs, ts => eval.ScriptTS = ts);
}
