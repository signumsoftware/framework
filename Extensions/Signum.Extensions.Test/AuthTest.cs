using Signum.Authorization;
using Signum.Authorization.Rules;
using System.Collections.Frozen;
using System.Collections.ObjectModel;

namespace Signum.Test.Authorization;

public class AuthTest
{
    [Fact]
    public void MergeMax()
    {

        var a = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
        {
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Spain}.ToFrozenSet(), TypeAllowed.Write),
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Germany}.ToFrozenSet(), TypeAllowed.Read)
        }.ToReadOnly()).WithPrima(false);

        var max = WithConditions<TypeAllowed>.Simple(TypeAllowed.Write).WithPrima(false);

        var result = TypeConditionMerger.MergeBaseImplementations(new() { a, max }, TypeCache.MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);

        Assert.Equal(max.RemovePrima(), result.RemovePrima());
    }

    [Fact]
    public void MergeMin()
    {

        var a = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
        {
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Spain}.ToFrozenSet(), TypeAllowed.Write),
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Germany}.ToFrozenSet(), TypeAllowed.Read)
        }.ToReadOnly()).WithPrima(false);

        var min = WithConditions<TypeAllowed>.Simple(TypeAllowed.None).WithPrima(false);

        var result = TypeConditionMerger.MergeBaseImplementations(new() { a, min }, TypeCache.MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);

        Assert.Equal(a, result);
    }

    [Fact]
    public void MergeIdentical()
    {

        var a = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
        {
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Spain}.ToFrozenSet(), TypeAllowed.Write),
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Germany}.ToFrozenSet(), TypeAllowed.Read)
        }.ToReadOnly()).WithPrima(false);

        var result = TypeConditionMerger.MergeBaseImplementations(new() { a, a }, TypeCache.MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);

        Assert.Equal(a.RemovePrima(), result.RemovePrima());
    }

    [Fact]
    public void MergeTypes()
    {
        var spain = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
        {
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Spain}.ToFrozenSet(), TypeAllowed.Write),
        }.ToReadOnly()).WithPrima(false);

        var germany = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
{
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Germany}.ToFrozenSet(), TypeAllowed.Read)
        }.ToReadOnly()).WithPrima(false);

        var result = TypeConditionMerger.MergeBaseImplementations(new() { spain, germany }, TypeCache.MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);

        var mix = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
        {
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Germany}.ToFrozenSet(), TypeAllowed.Read),
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Spain}.ToFrozenSet(), TypeAllowed.Write)
        }.ToReadOnly()).WithPrima(false);

        Assert.Equal(mix.RemovePrima(), result.RemovePrima());
    }

    [Fact]
    public void MergeTypesPrima()
    {
        var spain = new WithConditions<TypeAllowed>(TypeAllowed.Write, new[]
        {
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Spain}.ToFrozenSet(), TypeAllowed.Write),
        }.ToReadOnly());

        var simple = WithConditions<TypeAllowed>.Simple(TypeAllowed.Write).WithPrima(false);

        var resultSimple = TypeConditionMerger.MergeBaseImplementations(new() { simple, spain.WithPrima(false) }, TypeCache.MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);

        Assert.Equal(WithConditions<TypeAllowed>.Simple(TypeAllowed.Write), resultSimple.RemovePrima());

        var resultPrima = TypeConditionMerger.MergeBaseImplementations(new() { simple, spain.WithPrima(true) }, TypeCache.MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);

        Assert.Equal(spain, resultPrima.RemovePrima());
    }
}

[AutoInit]
public static class TestCondition
{
    public static TypeConditionSymbol Spain; 
    public static TypeConditionSymbol Germany;
}
