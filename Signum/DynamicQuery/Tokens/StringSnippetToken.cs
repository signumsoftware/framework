using Signum.Utilities.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Signum.DynamicQuery.Tokens;

internal class StringSnippetToken : QueryToken
{
    public StringSnippetToken(QueryToken parent)
    {
        if (parent.Type != typeof(string))
            throw new InvalidOperationException("invalid parent for StringSnippetToken");

        Parent = parent;
    }

    public override string? Format => null;

    public override string? Unit => null;

    public override Type Type => typeof(string);

    public override string Key => "Snippet";

    public override QueryToken? Parent { get; }

    public override QueryToken Clone() => new StringSnippetToken(this.Parent!);

    public override Implementations? GetImplementations() => null;

    public override PropertyRoute? GetPropertyRoute() => null;

    public override string? IsAllowed() => this.Parent!.IsAllowed();

    public override string NiceName() => QueryTokenMessage.MatchSnippet.NiceToString(this.Parent!.NiceName());

    public override string ToString() => QueryTokenMessage.MatchSnippet.NiceToString();

    static MethodInfo miFindSnippet = ReflectionTools.GetMethodInfo(() => Highlighter.FindSnippet(null!, null!, 0));

    public static Func<QueryToken, int> SnippetSize = q => 300;

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        var filters = context.Filters.EmptyIfNull().SelectMany(a => a.GetAllFilters())
            .Where(f => f.GetTokens().Any(t => t.Equals(this.Parent) || t is PgTsVectorColumnToken tsvt && tsvt.GetColumnsRoutes().Contains(this.Parent!.GetPropertyRoute())))
            .SelectMany(a => a.GetKeywords())
            .ToHashSet();

        var maxLenght = SnippetSize(this.Parent!);

        return Expression.Call(miFindSnippet, this.Parent!.BuildExpression(context), Expression.Constant(filters), Expression.Constant(maxLenght));
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options) => new List<QueryToken>();
}

public class Highlighter
{
    private class Packet
    {
        public required string Sentence;
        public double Density;
        public int Offset;
    }

    [return: NotNullIfNotNull("text")]
    public static string? FindSnippet(string? text, HashSet<string> words, int maxLength)
    {
        if (text == null)
            return null;

        var sentences = text.Replace("\r", "").Split('\n', '.').Select(a => a.Trim()).Where(a => a.HasText());

        var i = 0;
        var packets = sentences.Select(sentence => new Packet
        {
            Sentence = sentence,
            Density = ComputeDensity(words, sentence),
            Offset = i++
        }).OrderByDescending(packet => packet.Density);

        var list = new SortedList<int, string>();
        int length = 0;

        foreach (var packet in packets)
        {
            if (length >= maxLength)
            {
                break;
            }
            string sentence = packet.Sentence;
            list.Add(packet.Offset, sentence.Etc(maxLength - length));
            length += packet.Sentence.Length;
        }

        var sb = new List<string>();
        int previous = -1;

        foreach (var item in list)
        {
            var offset = item.Key;
            var sentence = item.Value;
            var separator = previous == -1 ? "" : previous + 1 == offset ? ". " : " (…) ";
            sb.Add(separator);
            previous = offset;
            sb.Add(sentence);
        }

        return sb.ToString("");
    }


    static double ComputeDensity(HashSet<string> words, string sentence)
    {
        if (sentence.Length == 0)
            return 0;

        //even if not higlighted, better to find sentences where a sub-string is found
        return words.Where(w => sentence.Contains(w, StringComparison.InvariantCultureIgnoreCase)).Sum(w => w.Length) / (double)sentence.Length;

        //var matches = WordRegex.Matches(sentence);
        //return (double)matches.Count(m => words.Contains(m.Value)) / matches.Count;
    }
}
