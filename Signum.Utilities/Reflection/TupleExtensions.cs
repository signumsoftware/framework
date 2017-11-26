using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;

namespace Signum.Utilities.Reflection
{
    
    public static class TupleReflection
    {
        public static bool IsTuple(Type type)
        {
            return type.IsGenericType && IsTupleDefinition(type.GetGenericTypeDefinition());
        }

        private static bool IsTupleDefinition(Type genericTypeDefinition)
        {
            return genericTypeDefinition == TupleOf(genericTypeDefinition.GetGenericArguments().Length);
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
                default: return null;
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
                case 0: return type.GetProperty("Item1");
                case 1: return type.GetProperty("Item2");
                case 2: return type.GetProperty("Item3");
                case 3: return type.GetProperty("Item4");
                case 4: return type.GetProperty("Item5");
                case 5: return type.GetProperty("Item6");
                case 6: return type.GetProperty("Item7");
                case 7: return type.GetProperty("Rest");
            }

            throw new ArgumentException("Property with index {0} not found on {1}".FormatWith(index, type.GetType()));
        }

        public static Type TupleChainType(IEnumerable<Type> tupleElementTypes)
        {
            int count = tupleElementTypes.Count();

            if (count == 0)
                throw new InvalidOperationException("typleElementTypes is empty"); 

            if (count >= 8)
                return TupleOf(8).MakeGenericType(tupleElementTypes.Take(7).And(TupleChainType(tupleElementTypes.Skip(7))).ToArray());

            return TupleOf(tupleElementTypes.Count()).MakeGenericType(tupleElementTypes.ToArray());
        }

        public static Expression TupleChainConstructor(IEnumerable<Expression> fieldExpressions)
        {
            int count  = fieldExpressions.Count();

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
                return TupleChainProperty(Expression.Property(expression, TupleProperty(expression.Type, 7)), index - 7);

            return Expression.Property(expression, TupleProperty(expression.Type, index));
        }
    }
}
