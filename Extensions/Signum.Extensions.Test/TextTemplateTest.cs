using Signum.Templating;
using System.Globalization;

namespace Signum.Test.Templating;

/// <summary>
/// Text templates rendered over an in-memory model (m:), so no database or query is needed.
/// </summary>
public class TextTemplateTest
{
    class GuestModel
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
    }

    class MeetingModel
    {
        public string? Title { get; set; }
        public int? Number { get; set; }
        public decimal Budget { get; set; }
        public DateTime Date { get; set; }
        public bool IsPublic { get; set; }
        public GuestModel? Owner { get; set; }
        public List<GuestModel> Guests { get; set; } = new();
    }

    static MeetingModel Kickoff() => new MeetingModel
    {
        Title = "Kickoff",
        Number = 3,
        Budget = 1234.5m,
        Date = new DateTime(2026, 11, 18),
        IsPublic = true,
        Owner = new GuestModel { Name = "Ann", Age = 50 },
        Guests =
        {
            new GuestModel { Name = "John", Age = 30 },
            new GuestModel { Name = "Mary", Age = 45 },
            new GuestModel { Name = null, Age = null },
        }
    };

    static string Render(string template, MeetingModel? model = null)
    {
        var block = TextTemplateParser.Parse(template, null, typeof(MeetingModel));

        return block.Print(new TextTemplateParameters(null, CultureInfo.InvariantCulture, null) { Model = model ?? Kickoff() });
    }

    static string Errors(string template)
    {
        TextTemplateParser.TryParse(template, null, typeof(MeetingModel), out var errors);
        return errors;
    }

    [Fact]
    public void PlainText()
    {
        Assert.Equal("Hello world", Render("Hello world"));
    }

    [Fact]
    public void Value()
    {
        Assert.Equal("Meeting Kickoff number 3", Render("Meeting @[m:Title] number @[m:Number]"));
    }

    [Fact]
    public void ValueNested()
    {
        Assert.Equal("Owner: Ann", Render("Owner: @[m:Owner.Name]"));
    }

    [Fact]
    public void ValueNull()
    {
        Assert.Equal("Owner: ", Render("Owner: @[m:Owner.Name]", new MeetingModel()));
    }

    [Fact]
    public void ValueFormat()
    {
        Assert.Equal("2026-11-18", Render("@[m:Date:yyyy-MM-dd]"));
        Assert.Equal("1,234.50", Render("@[m:Budget:N2]"));
        Assert.Equal("KICKOFF", Render("@[m:Title:upper]"));
    }

    [Fact]
    public void Declare()
    {
        Assert.Equal("Ann is 50", Render("@declare[m:Owner] as $o@[$o.Name] is @[$o.Age]"));
    }

    [Fact]
    public void IfBoolean()
    {
        Assert.Equal("public", Render("@if[m:IsPublic]public@endif"));
        Assert.Equal("", Render("@if[m:IsPublic]public@endif", new MeetingModel()));
    }

    [Fact]
    public void IfElse()
    {
        Assert.Equal("public", Render("@if[m:IsPublic]public@else private@endif"));
        Assert.Equal(" private", Render("@if[m:IsPublic]public@else private@endif", new MeetingModel()));
    }

    [Theory]
    [InlineData(1, "one")]
    [InlineData(2, "two")]
    [InlineData(7, "many")]
    public void IfElseIf(int number, string expected)
    {
        Assert.Equal(expected, Render("@if[m:Number=1]one@elseif[m:Number=2]two@else many@endif", new MeetingModel { Number = number }).Trim());
    }

    [Fact]
    public void IfComparison()
    {
        Assert.Equal("yes", Render("@if[m:Number > 2]yes@endif"));
        Assert.Equal("", Render("@if[m:Number >= 4]yes@endif"));
        Assert.Equal("yes", Render("@if[m:Title=Kickoff]yes@endif"));
        Assert.Equal("yes", Render("@if[m:Title^=Kick]yes@endif"));
        Assert.Equal("yes", Render("@if[m:Title*=cko]yes@endif"));
        Assert.Equal("", Render("@if[m:Title!=Kickoff]yes@endif"));
    }

    [Fact]
    public void IfAndOr()
    {
        Assert.Equal("yes", Render("@if[m:Number=3 && m:Title=Kickoff]yes@endif"));
        Assert.Equal("", Render("@if[m:Number=4 AND m:Title=Kickoff]yes@endif"));
        Assert.Equal("yes", Render("@if[m:Number=4 || m:Title=Kickoff]yes@endif"));
        Assert.Equal("", Render("@if[m:Number=4 OR m:Title=Other]yes@endif"));
    }

    [Theory]
    [InlineData(null, "empty")]
    [InlineData("", "empty")]
    [InlineData("Kickoff", "filled")]
    public void IfStringComparedToNothing(string? title, string expected)
    {
        //null can not be written, so nothing after the operator stands for both null and ""
        var model = new MeetingModel { Title = title };

        Assert.Equal(expected, Render("@if[m:Title!=]filled@else empty@endif", model).Trim());
        Assert.Equal(expected, Render("@if[m:Title=]empty@else filled@endif", model).Trim());
        Assert.Equal(expected, Render("@if[m:Title]filled@else empty@endif", model).Trim());
    }

    [Fact]
    public void IfNumberComparedToNothing()
    {
        Assert.Equal("set", Render("@if[m:Number!=]set@endif"));
        Assert.Equal("unset", Render("@if[m:Number=]unset@endif", new MeetingModel()));
    }

    [Fact]
    public void IfNested()
    {
        Assert.Equal("public and third", Render("@if[m:IsPublic]public@if[m:Number=3] and third@endif@endif"));
    }

    [Fact]
    public void Foreach()
    {
        Assert.Equal("John(30) Mary(45) () ", Render("@foreach[m:Guests] as $g@[$g.Name](@[$g.Age]) @endforeach"));
    }

    [Fact]
    public void ForeachEmpty()
    {
        Assert.Equal("Guests: .", Render("Guests: @foreach[m:Guests] as $g@[$g.Name]@endforeach.", new MeetingModel()));
    }

    [Fact]
    public void ForeachWithIf()
    {
        Assert.Equal("John Mary ", Render("@foreach[m:Guests] as $g@if[$g.Name!=]@[$g.Name] @endif@endforeach"));
        Assert.Equal("Mary ", Render("@foreach[m:Guests] as $g@if[$g.Age > 40]@[$g.Name] @endif@endforeach"));
    }

    [Fact]
    public void Any()
    {
        Assert.Equal("yes", Render("@if[m:Guests.Any($g => $g.Age > 40)]yes@endif"));
        Assert.Equal("", Render("@if[m:Guests.Any($g => $g.Age > 60)]yes@endif"));
        Assert.Equal("", Render("@if[m:Guests.Any($g => $g.Age > 0)]yes@endif", new MeetingModel()));
    }

    [Fact]
    public void All()
    {
        Assert.Equal("", Render("@if[m:Guests.All($g => $g.Name != )]yes@endif"));
        Assert.Equal("yes", Render("@if[m:Guests.All($g => $g.Age != 99)]yes@endif"));
        Assert.Equal("yes", Render("@if[m:Guests.All($g => $g.Age > 100)]yes@endif", new MeetingModel()));
    }

    [Fact]
    public void QuantifierCombined()
    {
        //Inside the lambda the separators belong to the lambda; outside they combine it with other conditions
        Assert.Equal("yes", Render("@if[m:IsPublic && m:Guests.Any($g => $g.Name = Mary || $g.Age > 90)]yes@endif"));
        Assert.Equal("", Render("@if[m:Number=4 || m:Guests.Any($g => $g.Name = Mary && $g.Age > 90)]yes@endif"));
    }

    [Fact]
    public void QuantifierParameterIsScopedToTheLambda()
    {
        Assert.Contains("$g", Errors("@if[m:Guests.Any($g => $g.Age > 40)]@[$g.Name]@endif"));
        Assert.Equal("", Errors("@if[m:Guests.Any($g => $g.Age > 40)]yes@endif@foreach[m:Guests] as $g@[$g.Name]@endforeach"));
    }

    [Fact]
    public void AnyKeywordIsRejected()
    {
        Assert.Contains("no longer supported", Errors("@any[m:Guests]yes@endany"));
    }

    [Fact]
    public void ParseErrors()
    {
        Assert.NotEqual("", Errors("@if[m:IsPublic]never closed"));
        Assert.NotEqual("", Errors("closed but never opened @endif"));
        Assert.NotEqual("", Errors("@[$undefined.Name]"));
        Assert.NotEqual("", Errors("@[m:DoesNotExist]"));
        Assert.NotEqual("", Errors("@if[m:Guests.Any(no lambda)]yes@endif"));
    }
}
