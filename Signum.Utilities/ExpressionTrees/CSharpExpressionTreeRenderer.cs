using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Utilidades.ExpressionTrees
{

    //acabar quote
    public static class CSharpTypeRenderer
    {
        static Dictionary<Type, string> basicTypes = new Dictionary<Type, string>()
         {
             {typeof(char), "char"},
             {typeof(sbyte), "sbyte"},
             {typeof(byte), "byte"},
             {typeof(short), "short"},
             {typeof(ushort), "ushort"},
             {typeof(int), "int"},
             {typeof(uint), "uint"},
             {typeof(long), "long"},
             {typeof(ulong), "ulong"},
             {typeof(string), "string"},
         };

        public static string MethodName(this MethodInfo method)
        {
            if (method.IsGenericMethodDefinition)
                throw new NotImplementedException();

            if (method.IsGenericMethodDefinition)
                return "{0}<{1}>".Formato(method.Name.Split('`')[0], method.GetGenericArguments().ToString(t => TypeName(t), ","));

            return method.Name;
        }

        public static string TypeName(this Type type)
        {
            if (type.IsGenericTypeDefinition)
                throw new NotImplementedException();

            string result = basicTypes.TryGetC(type);
            if (result != null)
                return result;

            if (type.IsArray)
                return "{0}[{1}]".Formato(TypeName(type.GetElementType()), new string(',', type.GetArrayRank() - 1));

            Type ut = Nullable.GetUnderlyingType(type);
            if (ut != null)
                return "{0}?".Formato(TypeName(ut));

            if (type.IsGenericType)
                return "{0}<{1}>".Formato(type.Name.Split('`')[0], type.GetGenericArguments().ToString(t => TypeName(t), ","));

            return type.Name;
        }
    }

    public static class CSharRendererVisitor
    {
        static List<ExpressionType> precedence = new List<ExpressionType>
        {   
            //elements
            ExpressionType.Parameter,
            ExpressionType.Quote,
            ExpressionType.Constant,
            //primary
            ExpressionType.ListInit,
            ExpressionType.New,
            ExpressionType.NewArrayInit,
            ExpressionType.NewArrayBounds,
            ExpressionType.ArrayIndex,
            ExpressionType.ArrayLength,
            ExpressionType.Convert,
            ExpressionType.ConvertChecked,
            ExpressionType.Invoke,
            ExpressionType.Call,
            ExpressionType.MemberAccess,
            ExpressionType.MemberInit,
            //unary
            ExpressionType.Negate,
            ExpressionType.UnaryPlus,
            ExpressionType.NegateChecked,
            ExpressionType.Not,
            //multiplicative
            ExpressionType.Divide, 
            ExpressionType.Modulo,    
            ExpressionType.Multiply,
            ExpressionType.MultiplyChecked,
            //aditive
            ExpressionType.Add,
            ExpressionType.AddChecked,
            ExpressionType.Subtract,
            ExpressionType.SubtractChecked,
            //shift
            ExpressionType.LeftShift, 
            ExpressionType.RightShift,
            //relational an type testing
            ExpressionType.GreaterThan,
            ExpressionType.GreaterThanOrEqual, 
            ExpressionType.LessThan,
            ExpressionType.LessThanOrEqual,
            ExpressionType.TypeAs,
            ExpressionType.TypeIs,
            //equality
            ExpressionType.Equal,  
            ExpressionType.NotEqual,
            //logical
            ExpressionType.And,
            ExpressionType.AndAlso,
            ExpressionType.ExclusiveOr, 
            ExpressionType.Or,
            ExpressionType.OrElse,
            //conditional
            ExpressionType.Conditional,
            //asignment

            ExpressionType.Coalesce,
            ExpressionType.Lambda,
        };


        public static string GenerateCSharpCode(this Expression expression)
        {
            return VisitReal(expression);
        }

        static string VisitReal(Expression exp)
        {
            if (exp == null)
                throw new ArgumentNullException("exp");

            switch (exp.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.ArrayIndex:
                case ExpressionType.Coalesce:
                case ExpressionType.Divide:
                case ExpressionType.Equal:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LeftShift:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.NotEqual:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.Power:
                case ExpressionType.RightShift:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return VisitBinary((BinaryExpression)exp);

                case ExpressionType.ArrayLength:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Negate:
                case ExpressionType.UnaryPlus:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return VisitUnary((UnaryExpression)exp);

                case ExpressionType.Call:
                    return VisitMethodCall((MethodCallExpression)exp);

                case ExpressionType.Conditional:
                    return VisitConditional((ConditionalExpression)exp);

                case ExpressionType.Constant:
                    return VisitConstant((ConstantExpression)exp);

                case ExpressionType.Invoke:
                    return VisitInvocation((InvocationExpression)exp);

                case ExpressionType.Lambda:
                    return VisitLambda((LambdaExpression)exp);

                case ExpressionType.ListInit:
                    return VisitListInit((ListInitExpression)exp);

                case ExpressionType.MemberAccess:
                    return VisitMemberAccess((MemberExpression)exp);

                case ExpressionType.MemberInit:
                    return VisitMemberInit((MemberInitExpression)exp);

                case ExpressionType.New:
                    return VisitNew((NewExpression)exp);

                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return VisitNewArray((NewArrayExpression)exp);

                case ExpressionType.Parameter:
                    return VisitParameter((ParameterExpression)exp);

                case ExpressionType.TypeIs:
                    return VisitTypeIs((TypeBinaryExpression)exp);
            }
            throw new Exception("UnhandledExpressionType");
        }

        static string Visit(Expression exp, ExpressionType nodeType)
        {
            string result =  VisitReal(exp);

            int prevIndex = precedence.IndexOf(nodeType);
            if (prevIndex == -1)
                throw new NotImplementedException("Not supported {0}".Formato(nodeType));
            int newIndex = precedence.IndexOf(exp.NodeType);
            if (newIndex == -1)
                throw new NotImplementedException("Not supported {0}".Formato(exp.NodeType));

            if (prevIndex < newIndex)
                return "({0})".Formato(result);
            else return result; 
        }

        static Dictionary<ExpressionType, string> binarySymbol = new Dictionary<ExpressionType, string>
        {
            { ExpressionType.Add, "{0}+{1}"},
            { ExpressionType.AddChecked, "{0}+{1}"},
            { ExpressionType.And, "{0}&{1}"},
            { ExpressionType.AndAlso, "{0}&&{1}"},
            { ExpressionType.ArrayIndex, "{0}[{1}]" },
            { ExpressionType.Coalesce, "{0}??{1}"},
            { ExpressionType.Divide, "{0}/{1}"}, 
            { ExpressionType.Equal, "{0}=={1}"}, 
            { ExpressionType.ExclusiveOr,"{0}^{1}"},
            { ExpressionType.GreaterThan,"{0}>{1}"},
            { ExpressionType.GreaterThanOrEqual,"{0}>={1}"},
            { ExpressionType.LeftShift,"{0}<<{1}"},
            { ExpressionType.LessThan,"{0}<{1}"},
            { ExpressionType.LessThanOrEqual,"{0}<={1}"},
            { ExpressionType.Modulo,"{0}%{1}"},
            { ExpressionType.Multiply,"{0}*{1}"},
            { ExpressionType.MultiplyChecked,"{0}*{1}"},
            { ExpressionType.NotEqual,"{0}!={1}"},
            { ExpressionType.Or,"{0}|{1}"},
            { ExpressionType.OrElse,"{0}||{1}"},
            { ExpressionType.RightShift,"{0}>>{1}"},
            { ExpressionType.Subtract,"{0}-{1}"},
            { ExpressionType.SubtractChecked,"{0}-{1}"}
        };

        static string VisitBinary(BinaryExpression b)
        {
            return binarySymbol.GetOrThrow(b.NodeType, "Node {0} is not supported as a binary expression")
                .Formato(Visit(b.Left, b.NodeType), Visit(b.Right, b.NodeType));
        }



        static string VisitConditional(ConditionalExpression c)
        {
            return "{0}?{1}:{2}".Formato(Visit(c.Test, c.NodeType), Visit(c.IfTrue, c.NodeType), Visit(c.IfFalse, c.NodeType));
        }

        static string VisitConstant(ConstantExpression c)
        {
            return c.Value.ToString();
        }

        static string VisitElementInitializer(ElementInit initializer)
        {
            if (initializer.Arguments.Count == 1)
                return VisitReal(initializer.Arguments[0]);
            else
                return @"{{{0}}}".Formato(VisitExpressionList(initializer.Arguments));
        }

        static string VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
        {
            return original.ToString(i => VisitElementInitializer(i), ", ");
        }

        static string VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            return original.ToString(e => VisitReal(e), ", ");
        }

        static string VisitInvocation(InvocationExpression iv)
        {
            return "{0}({1})".Formato(Visit(iv.Expression, iv.NodeType), VisitExpressionList(iv.Arguments));
        }

        static string VisitLambda(LambdaExpression lambda)
        {
            return "({0})=>{1}".Formato(
                lambda.Parameters.ToString(p => Visit(p, lambda.NodeType), ","),
                Visit(lambda.Body, lambda.NodeType));
        }

        static string VisitListInit(ListInitExpression init)
        {
            string newExpr = Visit(init.NewExpression, init.NodeType);
            if (newExpr.EndsWith("()"))
                newExpr = newExpr.RemoveRight(2);
            return @"{0}{{{1}}}".Formato(newExpr, VisitElementInitializerList(init.Initializers));
        }

        static string VisitMemberAccess(MemberExpression m)
        {
            if (m.Expression == null)
                return m.Member.Name;
            else
                return "{0}.{1}".Formato(Visit(m.Expression, m.NodeType), m.Member.Name);
        }

        static string VisitMemberInit(MemberInitExpression init)
        {
            string newExpr = Visit(init.NewExpression, init.NodeType);
            if (newExpr.EndsWith("()"))
                newExpr = newExpr.RemoveRight(2);
            return @"{0}{{{1}}}".Formato(newExpr, VisitBindingList(init.Bindings));
        }

        static string VisitBindingList(ReadOnlyCollection<MemberBinding> original)
        {
            return original.ToString(i => VisitBinding(i), ", ");
        }

        static string VisitBinding(MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return VisitMemberAssignment((MemberAssignment)binding);
                default:
                    throw new ApplicationException("Unexpected {0}".Formato(binding.BindingType));
            }
        }

        static string VisitMemberAssignment(MemberAssignment assignment)
        {
            return "{0} = {1}".Formato(assignment.Member.Name, VisitReal(assignment.Expression));
        }

        static string VisitMethodCall(MethodCallExpression m)
        {
            return "{0}.{1}({2})".Formato(
                m.Object.TryCC(o => Visit(o, m.NodeType)) ?? m.Method.DeclaringType.TypeName(),
                m.Method.MethodName(),
                m.Arguments.ToString(a => Visit(a, m.NodeType), ","));
        }

        static string VisitNew(NewExpression nex)
        {
            return "new {0}({1})".Formato(nex.Type.TypeName(), VisitExpressionList(nex.Arguments));
        }

        static string VisitNewArray(NewArrayExpression na)
        {
            string arrayType = na.Type.GetElementType().Name;

            if (na.NodeType == ExpressionType.NewArrayBounds)
                return "new {0}[{1}]".Formato(arrayType, VisitReal(na.Expressions.Single()));
            else
                return "new {0}[] {{{1}}}".Formato(arrayType, VisitExpressionList(na.Expressions));
        }

        static string VisitParameter(ParameterExpression p)
        {
            return p.Name;
        }

        static string VisitTypeIs(TypeBinaryExpression b)
        {
            return "{0} is {1}".Formato(Visit(b.Expression, b.NodeType), b.TypeOperand.TypeName());
        }

        static Dictionary<ExpressionType, string> unarySymbol = new Dictionary<ExpressionType, string>()
        {
            {ExpressionType.ArrayLength, "{0}.Length"},
            {ExpressionType.Convert, "{0}"},
            {ExpressionType.ConvertChecked, "{0}"},
            {ExpressionType.Negate, "-{0}"},
            {ExpressionType.UnaryPlus, "+{0}"},
            {ExpressionType.NegateChecked, "-{0}"},
            {ExpressionType.Not, "!{0}"},
            {ExpressionType.Quote, ""},
        };

        static string VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.TypeAs)
                return "{0} as {1}".Formato(Visit(u.Operand, u.NodeType), u.Type.Name);
            else
                return unarySymbol.GetOrThrow(u.NodeType, "The nodeType {0} is not supported as unary expression")
                    .Formato(Visit(u.Operand, u.NodeType));
        }
    }
}
