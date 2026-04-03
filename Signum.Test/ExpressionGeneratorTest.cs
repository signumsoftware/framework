
namespace Signum.Test;

public class ExpressionGeneratorTest
{
    [Fact]
    public void TestStatic()
    {   
        Assert.Equal(5, StaticExample.SumAuto(2, 3));
        Assert.Equal(6, StaticExample.SumMany(1, 1, 1, 1, 1, 1));
        Assert.Equal(5, StaticExample.SumManyIgnore(1, 1, 1, 1, 2, 3));
        Assert.Equal(30, StaticExample.SumWithLambda(10, 20));
        Assert.Equal("Hi", StaticExample.Bla);
    }

    [Fact]
    public void TestInstance()
    {
        Assert.Equal(7, new InstanceExample().NameLengthAuto(3));
        Assert.Equal("Mr.John", new InstanceExample().MrName);
    }

}

public class StaticExample
{
    [AutoExpressionField]
    public static int SumAuto(int a, int b) => As.Expression(() => a + b);

    [AutoExpressionField]
    public static int SumMany(int a, int b, int c, int d, int e, int f) => As.Expression(() => a + b + c + d + e + f);

    [AutoExpressionField]
    public static int SumManyIgnore(int a, int b, int c, int d, int e, int f) => As.Expression(() => f + e);

    [AutoExpressionField]
    public static int SumWithLambda(int a, int b) => As.Expression(() => a.Let(_a => _a + b));

    [AutoExpressionField]
    public static string Bla => As.Expression(() => "Hi");

    //static Expression<Func<string>> BlaExpression;
    //[ExpressionField("BlaExpression")]
    //public static string BlaManu => BlaExpression.Evaluate();

    //static StaticExample()
    //{
    //    BlaManuInit();
    //}

    //private static void BlaManuInit()
    //{
    //    BlaExpression = () => "Hi";
    //}



    //static Expression<Func<int, int, int>> SumManuExpression;
    //[ExpressionField("SumManuExpression")]
    //public static int SumManu(int a, int b) => SumManuExpression.Evaluate(a, b);

    //static StaticExample()
    //{
    //    SumManuInit();
    //}

    //private static void SumManuInit()
    //{
    //    SumManuExpression = (a, b) => a + b;
    //}

    //public static int SumKeep(int a, int b) => As.Expression(() => a + b);
}

public class InstanceExample
{
    string Name = "John";

    [AutoExpressionField]
    public int NameLengthAuto(int val) => As.Expression(() => Name.Length + val);

    public string MrName => "Mr." + this.Name;

    //static Expression<Func<InstanceExample, int, int>> NameLengthManuExpression;
    //[ExpressionField("NameLengthManu")]
    //public int NameLengthManu(int val) => NameLengthManuExpression.Evaluate(this, val);

    //static InstanceExample()
    //{
    //    NameLengthManuInit();
    //}

    //private static void NameLengthManuInit()
    //{
    //    NameLengthManuExpression = (@this, val) => @this.Name.Length + val;
    //}

    //public int NameLengthKeep(int val) => As.Expression(() => Name.Length + val);
}
