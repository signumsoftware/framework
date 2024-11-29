
namespace Signum.Utilities.Reflection;


public static class TupleReflection
{
    public static bool IsTuple(Type type)
    {
        return type.IsGenericType && IsTupleDefinition(type.GetGenericTypeDefinition());
    }

    private static bool IsTupleDefinition(Type genericTypeDefinition)
    {
        var numParameters = genericTypeDefinition.GetGenericArguments().Length;

        return numParameters <= 8 && genericTypeDefinition == TupleOf(numParameters);
    }

    public static Type TupleOf(int numParameters)
    {
        switch (numParameters)
        {
            case 1: return typeof(Tuple<>);
            case 2: return typeof(Tuple<,>);
            case 3: return typeof(Tuple<,,>);
            case 4: return typeof(Tuple<,,,>);
            case 5: return typeof(Tuple<,,,,>);
            case 6: return typeof(Tuple<,,,,,>);
            case 7: return typeof(Tuple<,,,,,,>);
            case 8: return typeof(Tuple<,,,,,,,>);
            default: throw new UnexpectedValueException(numParameters);
        }
    }

    public static int TupleIndex(PropertyInfo pi)
    {
        switch (pi.Name)
        {
            case "Item1": return 0;
            case "Item2": return 1;
            case "Item3": return 2;
            case "Item4": return 3;
            case "Item5": return 4;
            case "Item6": return 5;
            case "Item7": return 6;
            case "Rest": return 7;
        }

        throw new ArgumentException("pi should be the property of a Tuple type");
    }

    public static PropertyInfo TupleProperty(Type type, int index)
    {
        switch (index)
        {
            case 0: return type.GetProperty("Item1")!;
            case 1: return type.GetProperty("Item2")!;
            case 2: return type.GetProperty("Item3")!;
            case 3: return type.GetProperty("Item4")!;
            case 4: return type.GetProperty("Item5")!;
            case 5: return type.GetProperty("Item6")!;
            case 6: return type.GetProperty("Item7")!;
            case 7: return type.GetProperty("Rest")!;
        }

        throw new ArgumentException("Property with index {0} not found on {1}".FormatWith(index, type.GetType()));
    }

    public static Type TupleChainType(IEnumerable<Type> tupleElementTypes)
    {
        var list = tupleElementTypes.ToList();
        int count = list.Count();

        if (count == 0)
            throw new InvalidOperationException("typleElementTypes is empty"); 

        if (count >= 8)
            return TupleOf(8).MakeGenericType(list.Take(7).And(TupleChainType(list.Skip(7))).ToArray());

        return TupleOf(list.Count()).MakeGenericType(list.ToArray());
    }

    public static Expression TupleChainConstructor(IEnumerable<Expression> fieldExpressions)
    {
        var list = fieldExpressions.ToList();
        int count  = list.Count();

        if (count == 0)
            return Expression.Constant(new object(), typeof(object));

        Type type = TupleChainType(list.Select(e => e.Type));
        ConstructorInfo ci = type.GetConstructors().SingleEx();

        if (count >= 8)
            return Expression.New(ci, list.Take(7).And(TupleChainConstructor(list.Skip(7))));

        return Expression.New(ci, list); 
    }

    public static Expression TupleChainProperty(Expression expression, int index)
    {
        if (index >= 7)
            return TupleChainProperty(Expression.Property(expression, TupleProperty(expression.Type, 7)), index - 7);

        return Expression.Property(expression, TupleProperty(expression.Type, index));
    }
}

public static class ValueTupleReflection
{
    public static bool IsValueTuple(Type type)
    {
        return type.IsGenericType && IsValueTupleDefinition(type.GetGenericTypeDefinition());
    }

    private static bool IsValueTupleDefinition(Type genericTypeDefinition)
    {
        var numParameters = genericTypeDefinition.GetGenericArguments().Length;

        return numParameters <= 8 && genericTypeDefinition == ValueTupleOf(numParameters);
    }

    public static Type ValueTupleOf(int numParameters)
    {
        switch (numParameters)
        {
            case 1: return typeof(ValueTuple<>);
            case 2: return typeof(ValueTuple<,>);
            case 3: return typeof(ValueTuple<,,>);
            case 4: return typeof(ValueTuple<,,,>);
            case 5: return typeof(ValueTuple<,,,,>);
            case 6: return typeof(ValueTuple<,,,,,>);
            case 7: return typeof(ValueTuple<,,,,,,>);
            case 8: return typeof(ValueTuple<,,,,,,,>);
            default: throw new UnexpectedValueException(numParameters);
        }
    }

    public static int TupleIndex(FieldInfo pi)
    {
        switch (pi.Name)
        {
            case "Item1": return 0;
            case "Item2": return 1;
            case "Item3": return 2;
            case "Item4": return 3;
            case "Item5": return 4;
            case "Item6": return 5;
            case "Item7": return 6;
            case "Rest": return 7;
        }

        throw new ArgumentException("pi should be the property of a Tuple type");
    }

    public static FieldInfo TupleField(Type type, int index)
    {
        switch (index)
        {
            case 0: return type.GetField("Item1")!;
            case 1: return type.GetField("Item2")!;
            case 2: return type.GetField("Item3")!;
            case 3: return type.GetField("Item4")!;
            case 4: return type.GetField("Item5")!;
            case 5: return type.GetField("Item6")!;
            case 6: return type.GetField("Item7")!;
            case 7: return type.GetField("Rest")!;
        }

        throw new ArgumentException("Property with index {0} not found on {1}".FormatWith(index, type.GetType()));
    }

    public static Type TupleChainType(IEnumerable<Type> tupleElementTypes)
    {
        int count = tupleElementTypes.Count();

        if (count == 0)
            throw new InvalidOperationException("typleElementTypes is empty");

        if (count >= 8)
            return ValueTupleOf(8).MakeGenericType(tupleElementTypes.Take(7).And(TupleChainType(tupleElementTypes.Skip(7))).ToArray());

        return ValueTupleOf(tupleElementTypes.Count()).MakeGenericType(tupleElementTypes.ToArray());
    }

    public static Expression TupleChainConstructor(IEnumerable<Expression> fieldExpressions)
    {
        int count = fieldExpressions.Count();

        if (count == 0)
            return Expression.Constant(new object(), typeof(object));

        Type type = TupleChainType(fieldExpressions.Select(e => e.Type));
        ConstructorInfo ci = type.GetConstructors().SingleEx();

        if (count >= 8)
            return Expression.New(ci, fieldExpressions.Take(7).And(TupleChainConstructor(fieldExpressions.Skip(7))));

        return Expression.New(ci, fieldExpressions);
    }

    public static Expression TupleChainProperty(Expression expression, int index)
    {
        if (index >= 7)
            return TupleChainProperty(Expression.Field(expression, TupleField(expression.Type, 7)), index - 7);

        return Expression.Field(expression, TupleField(expression.Type, index));
    }
}
