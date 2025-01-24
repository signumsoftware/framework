
namespace Signum.Test;

public class StringDistanceTest
{
    StringDistance d = new StringDistance();

    [Fact]
    public void ResetLazy()
    {
        int i = 0;
        ResetLazy<string> val = new ResetLazy<string>(() => "hola" + i++);

        val.Reset(); //reset before initialized
        var str1 = val.Value;
        val.Reset(); //reset after initialized

        var str2 = val.Value;

        Assert.NotEqual(str1, str2);
    }

    [Fact]
    public void LevenshteinDistance()
    {
        Assert.Equal(1, d.LevenshteinDistance("hi", "ho"));
        Assert.Equal(1, d.LevenshteinDistance("hi", "hil"));
        Assert.Equal(1, d.LevenshteinDistance("hi", "h"));
    }

    [Fact]
    public void LevenshteinDistanceWeight()
    {
        Func<StringDistance.Choice<char>, int> w = c => c.HasAdded && char.IsNumber(c.Added) || c.HasRemoved && char.IsNumber(c.Removed) ? 10 : 1;

        Assert.Equal(10, d.LevenshteinDistance("hola", "ho5la", weight: w));
        Assert.Equal(10, d.LevenshteinDistance("ho5la", "hola", weight: w));
        Assert.Equal(10, d.LevenshteinDistance("ho5la", "hojla", weight: w));
        Assert.Equal(10, d.LevenshteinDistance("hojla", "ho5la", weight: w));
        Assert.Equal(10, d.LevenshteinDistance("ho5la", "ho6la", weight: w));
    }


    [Fact]
    public void LongestCommonSubsequence()
    {
        Assert.Equal(4, d.LongestCommonSubsequence("hallo", "halo"));
        Assert.Equal(7, d.LongestCommonSubsequence("SupeMan", "SuperMan"));
        Assert.Equal(0, d.LongestCommonSubsequence("aoa", ""));
    }

    [Fact]
    public void LongestCommonSubstring()
    {
        Assert.Equal(3, d.LongestCommonSubstring("hallo", "halo"));
        Assert.Equal(4, d.LongestCommonSubstring("SupeMan", "SuperMan"));
        Assert.Equal(0, d.LongestCommonSubstring("aoa", ""));
    }

    [Fact]
    public void Diff()
    {
        var result = d.Diff("en un lugar de la mancha".ToCharArray(), "in un place de la mincha".ToCharArray());

        var str = result.ToString("");

        Assert.Equal("-e+in un +pl-u-ga-r+c+e de la m-a+incha", str);
    }

    [Fact]
    public void DiffWords()
    {
        var result = d.DiffWords(
            "Soft drinks, coffees, teas, beers, and ginger ales",
            "Soft drinks, coffees, teas and beers");

        var str = result.ToString("");

        Assert.Equal("Soft drinks, coffees, teas-,- -beers-, and -ginger- -ales+beers", str);

    }

    [Fact]
    public void Choices()
    {
        var result = d.LevenshteinChoices("en un lugar de la mancha".ToCharArray(), "in un legarito de la mincha".ToCharArray());

        var str = result.ToString("");

        Assert.Equal("[-e+i]n un l[-u+e]gar+i+t+o de la m[-a+i]ncha", str);
    }

    [Fact]
    public void DiffText()
    {
        var text1 =
@"  Hola Pedro
Que tal
Bien

Adios Juan";

        var text2 =
@"  Hola Pedri

Que til
Adios Juani";

        var result = d.DiffText(text1, text2);

        var str = result.ToString(l => (l.Action == StringDistance.DiffAction.Added ? "[+]" :
            l.Action == StringDistance.DiffAction.Removed ? "[-]" : "[=]") + l.Value.ToString(""), "\n");

        Assert.Equal(
@"[=]  Hola -Pedro+Pedri
[-]-Que tal
[-]-Bien
[=]
[+]+Que til
[=]Adios -Juan+Juani", str);

    }

    [Fact]
    public void SmithWatermanScore()
    {
        Assert.Equal(4 * 2, d.SmithWatermanScore("Hola dola caracola", pattern: "dola"));

        Assert.Equal(3 * 2 - 1, d.SmithWatermanScore("Hola dola caracola", pattern: "dila"));

        Assert.Equal((3 * 2 - 1, "dola"), d.SmithWatermanScoreWithResult("Hola dola caracola", pattern: "dila"));

        Assert.Equal((3 * 2 - 1, "dla"), d.SmithWatermanScoreWithResult("Hola dola caracola", pattern: "dla"));

        Assert.Equal((7 * 2 - 1, "carcola"), d.SmithWatermanScoreWithResult("Hola dola carcola", pattern: "caracola"));
    }
}
