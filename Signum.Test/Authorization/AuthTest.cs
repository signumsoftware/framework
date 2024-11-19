using Signum.Authorization;
using Signum.Authorization.Rules;
using System.Collections.Frozen;
using System.Collections.ObjectModel;

namespace Signum.Test.Authorization;

public class AuthTest
{
    [Fact]
    void MergeMax()
    {

        var a = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
        {
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Spain}.ToFrozenSet(), TypeAllowed.Write),
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Germany}.ToFrozenSet(), TypeAllowed.Read)
        }.ToReadOnly());

        var max = new WithConditions<TypeAllowed>(TypeAllowed.Write);

        var result = ConditionMerger<TypeAllowed>.MergeBaseImplementations(new() { a, max }, TypeCache.MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);

        Assert.Equal(max, result);
    }

    [Fact]
    void MergeMin()
    {

        var a = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
        {
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Spain}.ToFrozenSet(), TypeAllowed.Write),
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Germany}.ToFrozenSet(), TypeAllowed.Read)
        }.ToReadOnly());

        var min = new WithConditions<TypeAllowed>(TypeAllowed.None);

        var result = ConditionMerger<TypeAllowed>.MergeBaseImplementations(new() { a, min }, TypeCache.MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);

        Assert.Equal(a, result);
    }

    [Fact]
    void MergeIdentical()
    {

        var a = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
        {
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Spain}.ToFrozenSet(), TypeAllowed.Write),
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Germany}.ToFrozenSet(), TypeAllowed.Read)
        }.ToReadOnly());

        var result = ConditionMerger<TypeAllowed>.MergeBaseImplementations(new() { a, a }, TypeCache.MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);

        Assert.Equal(a, result);
    }

    [Fact]
    void MergeTypes()
    {

        var germany = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
        {
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Spain}.ToFrozenSet(), TypeAllowed.Write),
        }.ToReadOnly());

        var spain = new WithConditions<TypeAllowed>(TypeAllowed.None, new[]
{
            new ConditionRule<TypeAllowed>(new []{ TestCondition.Germany}.ToFrozenSet(), TypeAllowed.Read)
        }.ToReadOnly());

        var result = ConditionMerger<TypeAllowed>.MergeBaseImplementations(new() { germany, spain }, TypeCache.MaxTypeAllowed, TypeAllowed.Write, TypeAllowed.None);

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
