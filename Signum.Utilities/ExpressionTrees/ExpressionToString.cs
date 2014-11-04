using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace Signum.Utilities.ExpressionTrees
{
    //This class wouldnt be neccessayr if Expression.BuildString where 'protected internal' instead of 'internal'
    internal class ExpressionToString : ExpressionVisitor
    {
        public enum Precedence
        {
            Primary, 
            Unary, 
            Multiplicative,
            Additive,
            Shift,
            RelationalTypeChecking,
            Equality,
            LAnd,
            LXor,
            LOr,
            CAnd,
            COr,
            NullCoalesce,
            Conditional,
            AssignmentLambda
        }


        public static Precedence? GetPrecedence(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.MemberAccess:
                case ExpressionType.Call:
                case ExpressionType.Invoke:
                case ExpressionType.ArrayIndex:
                case ExpressionType.ArrayLength:
                case ExpressionType.New:
                case ExpressionType.NewArrayBounds:
                case ExpressionType.NewArrayInit:
                case ExpressionType.MemberInit:
                case ExpressionType.ListInit:
                case ExpressionType.Constant:
                case ExpressionType.Parameter:
                    return Precedence.Primary;

                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Quote:
                case ExpressionType.Not:
                    return Precedence.Unary;

                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                    return Precedence.Multiplicative;

                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return Precedence.Additive;

                case ExpressionType.LeftShift:
                case ExpressionType.RightShift:
                    return Precedence.Shift;

                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.TypeAs:
                case ExpressionType.TypeIs:
                    return Precedence.RelationalTypeChecking;

                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    return Precedence.Equality;

                case ExpressionType.And:
                    return Precedence.LOr;

                case ExpressionType.ExclusiveOr:
                    return Precedence.LXor;

                case ExpressionType.Or:
                    return Precedence.LOr;

                case ExpressionType.AndAlso:
                    return Precedence.CAnd;

                case ExpressionType.OrElse:
                    return Precedence.COr;

                case ExpressionType.Coalesce:
                    return Precedence.NullCoalesce;

                case ExpressionType.Conditional:
                    return Precedence.Conditional;

                case ExpressionType.Lambda:
                    return Precedence.AssignmentLambda;
            }

            return null;
        }

        StringBuilder builder = new StringBuilder();

        int spaces = 0;

        public IDisposable Ident()
        {
            spaces += 3;
            return new Disposable(() => spaces -= 3); 
        }

        void AppendLine()
        {
            builder.AppendLine();
            builder.Append(new string(' ', spaces));
        }

        public static string NiceToString(Expression exp)
        {
            ExpressionToString ets = new ExpressionToString();
            ets.Visit(exp);
            return ets.builder.ToString(); 
        }

        protected Expression VisitParenthesis(Expression exp, Expression parent, bool associativePreference)
        {
            Precedence? pExp = GetPrecedence(exp.NodeType);
            Precedence? pParent = GetPrecedence(parent.NodeType);

            bool needParent = pExp.HasValue && pParent.HasValue && (associativePreference ? pExp.Value > pParent.Value : pExp.Value >= pParent.Value);

            if (needParent)
                builder.Append("(");

            Visit(exp);

            if (needParent)
                builder.Append(")");

            return exp;
        }

        public override Expression Visit(Expression exp)
        {
            if ( Enum.IsDefined(typeof(ExpressionType), exp.NodeType) && exp.NodeType <= ExpressionType.Assign)
                return base.Visit(exp);
            else
                builder.Append(exp.ToString());

            return exp;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.ArrayIndex)
            {
                VisitParenthesis(b.Left, b, true);
                builder.Append("[");
                Visit(b.Right);
                builder.Append("]");
            }
            else
            {
                string @operator = GetOperator(b);
                if (@operator != null)
                {
                    VisitParenthesis(b.Left, b, true);
                    builder.Append(" ");
                    builder.Append(@operator);
                    builder.Append(" ");
                    VisitParenthesis(b.Right, b, false);
                }
                else
                {
                    builder.Append(b.NodeType);
                    builder.Append("(");
                    Visit(b.Left);
                    builder.Append(", ");
                    Visit(b.Right);
                    builder.Append(")");
                }
            }
            return b;
        }

        private string GetOperator(BinaryExpression b)
        {
            switch (b.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked: return "+";
                case ExpressionType.And: return "&";
                case ExpressionType.AndAlso:return "&&";
                case ExpressionType.Coalesce:return "??";
                case ExpressionType.Divide:return "/";
                case ExpressionType.Equal:return "==";
                case ExpressionType.ExclusiveOr:return "^";
                case ExpressionType.GreaterThan:return ">";
                case ExpressionType.GreaterThanOrEqual:return ">=";
                case ExpressionType.LeftShift:return "<<";
                case ExpressionType.LessThan:return "<";
                case ExpressionType.LessThanOrEqual:return "<=";
                case ExpressionType.Modulo:return "%";
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:return "*";
                case ExpressionType.NotEqual:return "!=";
                case ExpressionType.Or: return  "|" ;
                case ExpressionType.OrElse:return "||";
                case ExpressionType.Power:return "^";
                case ExpressionType.RightShift:return ">>";
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:return "-";
            }
            return null;
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            VisitParenthesis(c.Test, c, false);
            builder.Append(" ? ");
            VisitParenthesis(c.IfTrue, c, true);
            builder.Append(" : ");
            VisitParenthesis(c.IfFalse, c, true);
            return c;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value != null)
            {
                if (c.Value is string)
                {
                    builder.Append("\"");
                    builder.Append(c.Value);
                    builder.Append("\"");
                }
                else if (c.Value.ToString() == c.Value.GetType().ToString() || typeof(IQueryable).IsAssignableFrom(c.Type))
                {
                    builder.Append("value(");
                    builder.Append(c.Value);
                    builder.Append(")");
                }
                else
                {
                    builder.Append(c.Value);
                }
            }
            else
            {
                builder.Append("null");
            }
            return c;
        }

        protected override ElementInit VisitElementInit(ElementInit initializer)
        {
            builder.Append(initializer.AddMethod);
            builder.Append("(");
            bool flag = true;
            foreach (Expression expression in initializer.Arguments)
            {
                if (flag)
                {
                    flag = false;
                }
                else
                {
                    builder.Append(",");
                }
                Visit(expression);
            }
            builder.Append(")");
            return initializer;
        }

        protected override Expression VisitInvocation(InvocationExpression iv)
        {
            VisitParenthesis(iv.Expression, iv, true);
            builder.Append("(");
            int num = 0;
            int count = iv.Arguments.Count;
            while (num < count)
            {
                if (num != 0)
                    builder.Append(",");
                Visit(iv.Arguments[num]);
                num++;
            }
            builder.Append(")");
            return iv;
        }

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            if (lambda.Parameters.Count == 1)
            {
                Visit(lambda.Parameters[0]);
            }
            else
            {
                builder.Append("(");
                int num = 0;
                int count = lambda.Parameters.Count;
                while (num < count)
                {
                    if (num > 0)
                    {
                        builder.Append(", ");
                    }
                    Visit(lambda.Parameters[num]);
                    num++;
                }
                builder.Append(")");
            }
            builder.Append(" => ");
            Visit(lambda.Body);
            return lambda;
        }

        protected override Expression VisitListInit(ListInitExpression init)
        {
            Visit(init.NewExpression);
            builder.Append(" {");
            int num = 0;
            int count = init.Initializers.Count;
            while (num < count)
            {
                if (num > 0)
                {
                    builder.Append(", ");
                }
                VisitElementInit(init.Initializers[num]);
                num++;
            }
            builder.Append("}");
            return init; 
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
        {
            builder.Append(assignment.Member.Name);
            builder.Append(" = ");
            Visit(assignment.Expression);
            return assignment;
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            if (m.Expression != null)
            {
                Visit(m.Expression);
            }
            else
            {
                builder.Append(m.Member.DeclaringType.TypeName());
            }
            builder.Append(".");
            builder.Append(m.Member.Name);
            return m;
        }

        protected override Expression VisitMemberInit(MemberInitExpression init)
        {
            if ((init.NewExpression.Arguments.Count == 0) && init.NewExpression.Type.Name.Contains("<"))
            {
                builder.Append("new");
            }
            else
            {
                Visit(init.NewExpression);
            }

            AppendLine();
            builder.Append("{");
            int num = 0;
            int count = init.Bindings.Count;
            using (Ident())
                while (num < count)
                {
                    MemberBinding binding = init.Bindings[num];
                    if (num > 0)
                    {
                        builder.Append(", ");
                    }
                    AppendLine();
                    VisitMemberBinding(binding);
                    num++;
                }

            AppendLine();
            builder.Append("}");
            return init;
        }

        protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
        {
            builder.Append(binding.Member.Name);
            builder.Append(" = {");
            int num = 0;
            int count = binding.Initializers.Count;
            while (num < count)
            {
                if (num > 0)
                {
                    builder.Append(", ");
                }
                VisitElementInit(binding.Initializers[num]);
                num++;
            }
            builder.Append("}");
            return binding;
        }

        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
        {
            builder.Append(binding.Member.Name);
            builder.Append(" = {");
            int i = 0;
            int count = binding.Bindings.Count;
            while (i < count)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                VisitMemberBinding(binding.Bindings[i]);
                i++;
            }
            builder.Append("}");
            return binding;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            int num = 0;
            Expression expression = m.Object;
            if (Attribute.GetCustomAttribute(m.Method, typeof(ExtensionAttribute)) != null)
            {
                num = 1;
                expression = m.Arguments[0];
            }
            if (expression != null)
            {
                Visit(expression);
                builder.Append(".");
            }
            builder.Append(m.Method.Name);
            builder.Append("(");
            int i = num;
            int count = m.Arguments.Count;
            while (i < count)
            {
                if (i > num)
                {
                    builder.Append(", ");
                }
                Visit(m.Arguments[i]);
                i++;
            }
            builder.Append(")");
            return m;
        }

        protected override Expression VisitNewArray(NewArrayExpression na)
        {
            switch (na.NodeType)
            {
                case ExpressionType.NewArrayInit:
                    {
                        builder.Append("new ");
                        builder.Append("[] {");
                        int i = 0;
                        int count = na.Expressions.Count;
                        while (i < count)
                        {
                            if (i > 0)
                            {
                                builder.Append(", ");
                            }
                            Visit(na.Expressions[i]);
                            i++;
                        }
                        builder.Append("}");
                        break;
                    }
                case ExpressionType.NewArrayBounds:
                    {
                        builder.Append("new ");
                        builder.Append(na.Type.TypeName());
                        builder.Append("(");
                        int i = 0;
                        int count = na.Expressions.Count;
                        while (i < count)
                        {
                            if (i > 0)
                            {
                                builder.Append(", ");
                            }
                            Visit(na.Expressions[i]);
                            i++;
                        }
                        builder.Append(")");
                        break;
                    }
            }
            return na;
        }

        protected override Expression VisitNew(NewExpression nex)
        {
            Type type;
            type = (nex.Constructor == null) ? (type = nex.Type) : nex.Constructor.DeclaringType;
            builder.Append("new ");
            int count = nex.Arguments.Count;
            builder.Append(type.TypeName());
            builder.Append("(");
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }
                    if (nex.Members != null)
                    {
                        PropertyInfo info = GetPropertyNoThrow(nex.Members[i] as MethodInfo);
                        if (info != null)
                        {
                            builder.Append(info.Name);
                        }
                        else
                        {
                            builder.Append(nex.Members[i].Name);
                        }
                        builder.Append(" = ");
                    }
                    Visit(nex.Arguments[i]);
                }
            }
            builder.Append(")");
            return nex;
        }

        static PropertyInfo GetPropertyNoThrow(MethodInfo method)
        {
            if (method != null)
            {
                Type declaringType = method.DeclaringType;
                BindingFlags bindingAttr = BindingFlags.NonPublic | BindingFlags.Public | 
                    (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance);
                foreach (PropertyInfo info in declaringType.GetProperties(bindingAttr))
                {
                    if (info.CanRead && (method == info.GetGetMethod(true)))
                    {
                        return info;
                    }
                    if (info.CanWrite && (method == info.GetSetMethod(true)))
                    {
                        return info;
                    }
                }
            }
            return null;
        }


        protected override Expression VisitParameter(ParameterExpression p)
        {
            if (p.Name != null)
            {
                builder.Append(p.Name);
            }
            else
            {
                builder.Append("<param>");
            }
            return p;
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    builder.Append("-");
                    VisitParenthesis(u.Operand, u, false);
                    break;
                case ExpressionType.UnaryPlus:
                    builder.Append("+");
                    VisitParenthesis(u.Operand, u, false);
                    break;
                case ExpressionType.Not:
                    if (u.Type.UnNullify() == typeof(bool))
                        builder.Append("!");
                    else
                        builder.Append("~");
                    VisitParenthesis(u.Operand, u, false);
                    break;
                case ExpressionType.Quote:
                    Visit(u.Operand);
                    break;
                case ExpressionType.TypeAs:
                    VisitParenthesis(u.Operand, u, false);
                    builder.Append(" as ");
                    builder.Append(u.Type.TypeName());
                    break;
                case ExpressionType.Convert:
                    builder.Append("((");
                    builder.Append(u.Type.TypeName());
                    builder.Append(")");
                    Visit(u.Operand);
                    builder.Append(")");
                    break;
                default:
                    builder.Append(u.NodeType);
                    builder.Append("(");
                    Visit(u.Operand);
                    builder.Append(")");
                    break;
            }

            return u;
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression b)
        {
            VisitParenthesis(b.Expression, b, false);
            builder.Append(" is ");
            builder.Append(b.TypeOperand.TypeName());
            return b;
        } 
    }
}
