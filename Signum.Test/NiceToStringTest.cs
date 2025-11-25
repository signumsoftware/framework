
namespace Signum.Test;

[DescriptionOptions(DescriptionOptions.Members)]
public enum EnumPruebas
{
    Test,
    [System.ComponentModel.Description("Test!")]
    Test2,
    MyTest,
    My_Test,
    TEST,
    YouAreFromONU,
    ILoveYou,
    YoYTu,
    B2C,
}

public class NiceToStringTest
{
    [Fact]
    public void EnumToStr()
    {
        Assert.Equal("B 2 C", EnumPruebas.B2C.ToString().SpacePascal());
        Assert.Equal("Test",         EnumPruebas.Test.NiceToString());
        Assert.Equal("Test!",        EnumPruebas.Test2.NiceToString());
        Assert.Equal("My test",      EnumPruebas.MyTest.NiceToString());
        Assert.Equal("My Test",      EnumPruebas.My_Test.NiceToString());
        Assert.Equal("TEST",         EnumPruebas.TEST.NiceToString());
        Assert.Equal("You are from ONU", EnumPruebas.YouAreFromONU.NiceToString());
        Assert.Equal("I love you", EnumPruebas.ILoveYou.NiceToString());
        Assert.Equal("Yo y tu", EnumPruebas.YoYTu.NiceToString());
    }

    [Fact]
    public void ExpressionToStr()
    {
        string str = Expression.Add(Expression.Constant(2), new MyExpression()).ToString();
        Assert.Contains("$$", str);
    }

    class MyExpression: Expression
    {
        public override Type Type { get{ return typeof(int);} }
        public override ExpressionType NodeType { get { return ExpressionType.Extension; } }

        public override string ToString()
        {
            return "$$";
        }
    }


    [Fact]
    public void ForNumber()
    {
        var cats = "{0} Cat[s]";

        Assert.Equal("0 Cats", cats.ForGenderAndNumber(number: 0).FormatWith(0));
        Assert.Equal("1 Cat", cats.ForGenderAndNumber(number: 1).FormatWith(1));
        Assert.Equal("2 Cats", cats.ForGenderAndNumber(number: 2).FormatWith(2));

        Assert.Equal("0 Cats", cats.ForGenderAndNumber(number: 0).FormatWith(0));
        Assert.Equal("1 Cat", cats.ForGenderAndNumber(number: 1).FormatWith(1));
        Assert.Equal("2 Cats", cats.ForGenderAndNumber(number: 2).FormatWith(2));
    }

    [Fact]
    public void ForGenderAndNumber()
    {
        var manWoman = "{0} [1m:Man|m:Men|1f:Woman|f:Women]";

        Assert.Equal("0 Men", manWoman.ForGenderAndNumber(gender: 'm', number: 0).FormatWith(0));
        Assert.Equal("1 Man", manWoman.ForGenderAndNumber(gender: 'm', number: 1).FormatWith(1));
        Assert.Equal("2 Men", manWoman.ForGenderAndNumber(gender: 'm', number: 2).FormatWith(2));

        Assert.Equal("0 Women", manWoman.ForGenderAndNumber(gender: 'f', number: 0).FormatWith(0));
        Assert.Equal("1 Woman", manWoman.ForGenderAndNumber(gender: 'f', number: 1).FormatWith(1));
        Assert.Equal("2 Women", manWoman.ForGenderAndNumber(gender: 'f', number: 2).FormatWith(2));
    }


    [Fact]
    public void IndentEndLine()
    {
      Assert.Equal(
          @"  hola\n
  dola\r\n
  juanola\n", @"hola\n
dola\r\n
juanola\n".Indent(2));

    }

    [Fact]
    public void PascalToSnake()
    {
        Assert.Equal("pascal_case_example", "PascalCaseExample".PascalToSnake());
        Assert.Equal("camel_case_example", "camelCaseExample".PascalToSnake());
        Assert.Equal("xml_parser", "XMLParser".PascalToSnake());
        Assert.Equal("http_request", "HTTPRequest".PascalToSnake());
        Assert.Equal("area51_exists", "Area51Exists".PascalToSnake());
    }
}
