namespace Signum.Upgrade;

public static class RegexExtensions
{
    public static Regex WithMacros(this Regex regex)
    {
        var pattern = regex.ToString();

        var newPattern = pattern
            .Replace("::EXPR::", new Regex(@"(?:[^()]|(?<Open>[(])|(?<-Open>[)]))+").ToString()) /*Just for validation and coloring*/
            .Replace("::IDENT::", new Regex(@"[$A-Z_][0-9A-Z_$]*").ToString());

        return new Regex(newPattern, regex.Options);
    }
}
