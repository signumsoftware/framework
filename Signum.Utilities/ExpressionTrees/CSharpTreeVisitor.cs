using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using System.Reflection;
using System.CodeDom;
using Microsoft.CSharp;
using System.IO;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using Signum.Utilities.Reflection;

namespace Signum.Utilities.ExpressionTrees
{
    internal class CSharpTreeVisitor
    {
        static List<ExpressionType> precedence = new List<ExpressionType>
        {   
            //elements
            ExpressionType.Parameter,
            ExpressionType.Quote,
            ExpressionType.Constant,
            //primary
            ExpressionType.New,
            ExpressionType.NewArrayInit,
            ExpressionType.ListInit,

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

        internal string[] ImportedNamespaces { get; set; } 

        internal string VisitReal(Expression exp)
        {
            if (exp == null)
                throw new ArgumentNullException("exp");

            bool collapse = false;
            bool literal = false; 
            if (exp.NodeType == ExpressionType.Call)
            {
                MethodCallExpression mc = (MethodCallExpression)exp;
                if (mc.Method.Name == "Literal" && mc.Method.DeclaringType == typeof(CSharpRenderer))
                {
                    exp = mc.Arguments[0];
                    literal = true;
                }

                if (mc.Method.Name == "Collapse" && mc.Method.DeclaringType == typeof(CSharpRenderer))
                {
                    exp = mc.Arguments[0];
                    collapse = true; 
                }
            }

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
                    return VisitConstant((ConstantExpression)exp, literal);

                case ExpressionType.Invoke:
                    return VisitInvocation((InvocationExpression)exp);

                case ExpressionType.Lambda:
                    return VisitLambda((LambdaExpression)exp);

                case ExpressionType.ListInit:
                    return VisitListInit((ListInitExpression)exp, collapse);

                case ExpressionType.MemberAccess:
                    return VisitMemberAccess((MemberExpression)exp, literal);

                case ExpressionType.MemberInit:
                    return VisitMemberInit((MemberInitExpression)exp, collapse);

                case ExpressionType.New:
                    return VisitNew((NewExpression)exp);

                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return VisitNewArray((NewArrayExpression)exp, collapse);

                case ExpressionType.Parameter:
                    return VisitParameter((ParameterExpression)exp);

                case ExpressionType.TypeIs:
                    return VisitTypeIs((TypeBinaryExpression)exp);
            }
            throw new Exception("UnhandledExpressionType");
        }

        string Visit(Expression exp, ExpressionType nodeType)
        {
            string result =  VisitReal(exp);

            int prevIndex = precedence.IndexOf(nodeType);
            if (prevIndex == -1)
                throw new NotImplementedException("Not supported {0}".FormatWith(nodeType));
            int newIndex = precedence.IndexOf(exp.NodeType);
            if (newIndex == -1)
                throw new NotImplementedException("Not supported {0}".FormatWith(exp.NodeType));

            if (prevIndex < newIndex)
                return "({0})".FormatWith(result);
            else return result; 
        }


        #region Simple
        Dictionary<ExpressionType, string> unarySymbol = new Dictionary<ExpressionType, string>()
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

        string VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.TypeAs)
                return "{0} as {1}".FormatWith(Visit(u.Operand, u.NodeType), u.Type.Name);
            else
                return unarySymbol.GetOrThrow(u.NodeType, "The node type {0} is not supported in unary expressions")
                    .FormatWith(Visit(u.Operand, u.NodeType));
        }

        string VisitTypeIs(TypeBinaryExpression b)
        {
            return "{0} is {1}".FormatWith(Visit(b.Expression, b.NodeType), b.TypeOperand.TypeName());
        }

        Dictionary<ExpressionType, string> binarySymbol = new Dictionary<ExpressionType, string>
        {
            { ExpressionType.Add, "{0} + {1}"},
            { ExpressionType.AddChecked, "{0} + {1}"},
            { ExpressionType.And, "{0} & {1}"},
            { ExpressionType.AndAlso, "{0} && {1}"},
            { ExpressionType.ArrayIndex, "{0}[{1}]" },
            { ExpressionType.Coalesce, "{0} ?? {1}"},
            { ExpressionType.Divide, "{0} / {1}"}, 
            { ExpressionType.Equal, "{0} == {1}"}, 
            { ExpressionType.ExclusiveOr,"{0} ^ {1}"},
            { ExpressionType.GreaterThan,"{0} > {1}"},
            { ExpressionType.GreaterThanOrEqual,"{0} >= {1}"},
            { ExpressionType.LeftShift,"{0} << {1}"},
            { ExpressionType.LessThan,"{0} < {1}"},
            { ExpressionType.LessThanOrEqual,"{0} <= {1}"},
            { ExpressionType.Modulo,"{0} % {1}"},
            { ExpressionType.Multiply,"{0} * {1}"},
            { ExpressionType.MultiplyChecked,"{0} * {1}"},
            { ExpressionType.NotEqual,"{0} != {1}"},
            { ExpressionType.Or,"{0} | {1}"},
            { ExpressionType.OrElse,"{0} || {1}"},
            { ExpressionType.RightShift,"{0} >> {1}"},
            { ExpressionType.Subtract,"{0} - {1}"},
            { ExpressionType.SubtractChecked,"{0} - {1}"}
        };

        string VisitBinary(BinaryExpression b)
        {
            return binarySymbol.GetOrThrow(b.NodeType, "The node type {0} is not supported as a binary expression")
                .FormatWith(Visit(b.Left, b.NodeType), Visit(b.Right, b.NodeType));
        }

        string VisitConditional(ConditionalExpression c)
        {
            return "{0} ? {1} : {2}".FormatWith(Visit(c.Test, c.NodeType), Visit(c.IfTrue, c.NodeType), Visit(c.IfFalse, c.NodeType));
        }

        string VisitMemberAccess(MemberExpression m, bool literal)
        {
            if (m.Expression == null)
                return m.Member.Name;
            else if (m.Expression is ConstantExpression)
            {
                object obj = ((ConstantExpression)m.Expression).Value;

                object value = m.Member is FieldInfo ? ((FieldInfo)m.Member).GetValue(obj) : ((PropertyInfo)m.Member).GetValue(obj, null);
                if (literal)
                    return value == null ? "null" : value.ToString();
                else
                    return CSharpRenderer.Value(value, value?.Let(v=>v.GetType()), ImportedNamespaces);
            }
            else
                return "{0}.{1}".FormatWith(Visit(m.Expression, m.NodeType), m.Member.Name);
        }

        string VisitConstant(ConstantExpression c, bool literal)
        {
            if (literal)
                return c.Value.ToString();
            else
                return CSharpRenderer.Value(c.Value, c.Type, ImportedNamespaces);
        }

        string VisitParameter(ParameterExpression p)
        {
            return p.Name;
        }

        #endregion

        #region Colecciones
        const int IndentationSpaces = 4;

        string Line<T>(ReadOnlyCollection<T> collection, Func<T, string> func)
        {
            return collection.ToString(func, ", ");
        }

        string Block<T>(ReadOnlyCollection<T> collection, Func<T, string> func, bool collapse)
        {
            if (collection.Count == 0)
                return "{ }";
            if(collapse)
                return "{{ {0} }}".FormatWith(collection.ToString(func, ", "));

            return "\r\n{{\r\n{0}\r\n}}".FormatWith(collection.ToString(func, ",\r\n").Indent(IndentationSpaces));
        }
        #endregion
        
        #region Elements
        string VisitBinding(MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return VisitMemberAssignment((MemberAssignment)binding);
                default:
                    throw new NotSupportedException("Unexpected {0}".FormatWith(binding.BindingType));
            }
        }

        string VisitMemberAssignment(MemberAssignment assignment)
        {
            return "{0} = {1}".FormatWith(assignment.Member.Name, VisitReal(assignment.Expression));
        }

        string VisitElementInitializer(ElementInit initializer)
        {
            if (initializer.Arguments.Count == 1)
                return VisitReal(initializer.Arguments[0]);
            else
                return Block(initializer.Arguments, VisitReal, true);
        }

        #endregion

        #region Collection Containers
        string VisitMemberInit(MemberInitExpression init, bool collapse)
        {
            string newExpr = Visit(init.NewExpression, init.NodeType);
            if (newExpr.EndsWith("()"))
                newExpr = newExpr.RemoveEnd(2);
            return @"{0} {1}".FormatWith(newExpr, Block(init.Bindings, VisitBinding, collapse));
        }

        string VisitListInit(ListInitExpression init, bool collapse)
        {
            string newExpr = Visit(init.NewExpression, init.NodeType);
            if (newExpr.EndsWith("()"))
                newExpr = newExpr.RemoveEnd(2);
            return @"{0} {1}".FormatWith(newExpr, Block(init.Initializers, VisitElementInitializer, collapse));
        }

        string VisitInvocation(InvocationExpression iv)
        {
            return "{0}({1})".FormatWith(Visit(iv.Expression, iv.NodeType), Line(iv.Arguments, VisitReal));
        }

        string VisitMethodCall(MethodCallExpression m)
        {
            return "{0}.{1}({2})".FormatWith(
                m.Object != null ? Visit(m.Object, m.NodeType) : m.Method.DeclaringType.TypeName(),
                m.Method.MethodName(),
                Line(m.Arguments, VisitReal));
        }

        string VisitNew(NewExpression nex)
        {
            return "new {0}({1})".FormatWith(nex.Type.TypeName(), Line(nex.Arguments, VisitReal));
        }

        string VisitNewArray(NewArrayExpression na, bool collapse)
        {
            string arrayType = na.Type.GetElementType().Name;

            if (na.NodeType == ExpressionType.NewArrayBounds)
                return "new {0}[{1}]".FormatWith(arrayType, VisitReal(na.Expressions.SingleEx()));
            else
                return "new {0}[] {1}".FormatWith(arrayType, Block(na.Expressions, VisitReal, collapse));
        } 
        #endregion

        string VisitLambda(LambdaExpression lambda)
        {
            string body = Visit(lambda.Body, lambda.NodeType);
            if (lambda.Parameters.Count == 1)
                return "{0} => {1}".FormatWith(Visit(lambda.Parameters.SingleEx(), lambda.NodeType), body);
            else
                return "({0}) => {1}".FormatWith(lambda.Parameters.ToString(p => Visit(p, lambda.NodeType), ","), body);
        }
    }
}
