using System.Text.RegularExpressions;

namespace Signum.Omnibox;

public class SpecialOmniboxResult : OmniboxResult
{
    public OmniboxMatch Match { get; set; }

    public string Key { get { return ((ISpecialOmniboxAction)Match.Value).Key; } }

    public override string ToString()
    {
        return "!" + this.Key;
    }
}

public interface ISpecialOmniboxAction
{
    string Key { get; }
    Func<bool> Allowed { get; }
}

public class SpecialOmniboxGenerator<T> : OmniboxResultGenerator<SpecialOmniboxResult> where T : ISpecialOmniboxAction
{
    public Dictionary<string, T> Actions;

    Regex regex = new Regex(@"^!I?$", RegexOptions.ExplicitCapture);

    public override IEnumerable<SpecialOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
    {
        if (!regex.IsMatch(tokenPattern))
            return Enumerable.Empty<SpecialOmniboxResult>();

        string ident = tokens.Count == 1 ? "" : tokens[1].Value;

        bool isPascalCase = OmniboxUtils.IsPascalCasePattern(ident);

        return OmniboxUtils.Matches(Actions, a => a.Allowed(), ident, isPascalCase)
            .Select(m => new SpecialOmniboxResult { Match = m, Distance = m.Distance });
    }

    public override List<HelpOmniboxResult> GetHelp()
    {
        return new List<HelpOmniboxResult>
        {
            new HelpOmniboxResult { Text = "!SpecialFunction", ReferencedType = typeof(SpecialOmniboxResult) },
        };
    }
}
