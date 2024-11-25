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
        }.ToReadOnly());

        var max = WithConditions<TypeAllowed>.Simple(TypeAllowed.Write);

        var result = TypeConditionMerger.MergeBaseImplementations(new() { a, max }, TypeCache.MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);

        Assert.Equal(max, result);
    }

    [Fact]
    public void MergeMin()
    {

        var a = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
        {
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Spain}.ToFrozenSet(), TypeAllowed.Write),
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Germany}.ToFrozenSet(), TypeAllowed.Read)
        }.ToReadOnly());

        var min = WithConditions<TypeAllowed>.Simple(TypeAllowed.None);

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
        }.ToReadOnly());

        var result = TypeConditionMerger.MergeBaseImplementations(new() { a, a }, TypeCache.MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);

        Assert.Equal(a, result);
    }

    [Fact]
    public void MergeTypes()
    {

        var germany = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
        {
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Spain}.ToFrozenSet(), TypeAllowed.Write),
        }.ToReadOnly());

        var spain = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
{
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Germany}.ToFrozenSet(), TypeAllowed.Read)
        }.ToReadOnly());

        var result = TypeConditionMerger.MergeBaseImplementations(new() { germany, spain }, TypeCache.MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);

        var mix = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
        {
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Germany}.ToFrozenSet(), TypeAllowed.Read),
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Spain}.ToFrozenSet(), TypeAllowed.Write)
        }.ToReadOnly());

        Assert.Equal(mix, result);
    }
}

[AutoInit]
public static class TestCondition
{
    public static TypeConditionSymbol Spain; 
    public static TypeConditionSymbol Germany;
}
