using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Reflection;

namespace Signum.Windows.UIAutomation
{
    public class ConditionBuilder :
        ExpressionVisitor
    {
        public enum AutomationExpressionType
        {
            Condition = 1000,
            Property,
            Pattern,
            Current,
        }

        abstract class AutomationExpresion : Expression
        {
            readonly Type type;
            public override Type Type
            {
                get { return type; }
            }

            readonly AutomationExpressionType nodeType;
            public override ExpressionType NodeType
            {
                get { return (ExpressionType)nodeType; }
            }

            protected AutomationExpresion(AutomationExpressionType nodeType, Type type)
            {
                this.type = type;
                this.nodeType = nodeType;
            }

            public abstract override string ToString();
        }

        class AutomationConditionExpression : AutomationExpresion
        {  
            public readonly Condition AutomationCondition;

            public AutomationConditionExpression(Condition condition): base(AutomationExpressionType.Condition, typeof(bool)) 
            {   
                this.AutomationCondition = condition;
            }

            public override string ToString()
            {
                return "Condition({0})".FormatWith(AutomationCondition.NiceToString());
            }
        }

        class AutomationPropertyExpression : AutomationExpresion
        {
            public readonly AutomationProperty AutomationProperty;

            public AutomationPropertyExpression(AutomationProperty property, Type type) : base(AutomationExpressionType.Property, type)
            {
                this.AutomationProperty = property;
            }

            public override string ToString()
            {
                return "Property({0})".FormatWith(AutomationProperty);
            }
        }

        class AutomationPatternExpression : AutomationExpresion
        {
            public readonly Type AutomationPattern;

            public AutomationPatternExpression(Type pattern, Type type) : base(AutomationExpressionType.Pattern, type)
            {
                this.AutomationPattern = pattern;
            }

            public override string ToString()
            {
                return "Pattern<{0}>".FormatWith(AutomationPattern.Name);
            }
        }

        class AutomationCurrentExpression : AutomationExpresion
        {
            public AutomationCurrentExpression(Type type)
                : base(AutomationExpressionType.Current, type)
            {
            }

            public override string ToString()
            {
                return "Current";
            }
        }

        ParameterExpression parameter;

        public static Condition ToCondition(Expression<Func<AutomationElement, bool>> condition)
        {
            Expression<Func<AutomationElement, bool>> clean = (Expression<Func<AutomationElement, bool>>)ExpressionCleaner.Clean(condition);

            ConditionBuilder builder = new ConditionBuilder { parameter = clean.Parameters.Single() };

            Expression expression = builder.Visit(clean.Body);

            return AsCondition(expression);
        }

        public override Expression Visit(Expression node)
        {
            var result = base.Visit(node);
            if (result.Type == typeof(bool) && !(result is AutomationConditionExpression || result is AutomationPropertyExpression))
                throw new InvalidOperationException("Impossible to translate to UIAutomation condition: {0}".FormatWith(node.ToString()));

            return result;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type == typeof(bool))
                return new AutomationConditionExpression(((bool)node.Value) ? Condition.TrueCondition : Condition.FalseCondition);

            return base.VisitConstant(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            var operand = Visit(node.Operand);

            if (node.NodeType == ExpressionType.Not)
                return new AutomationConditionExpression(new NotCondition(AsCondition(operand)));

            return Expression.MakeUnary(node.NodeType, operand, node.Type);
        }

        public static Condition AsCondition(Expression operand)
        {
            if (operand is AutomationConditionExpression ace)
                return ace.AutomationCondition;

            if (operand is AutomationPropertyExpression ape)
                return new PropertyCondition(ape.AutomationProperty, true);

            throw new InvalidOperationException("{0} is not a Condition");
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = Visit(node.Left);
            var right = Visit(node.Right);

            if (node.NodeType == ExpressionType.And || node.NodeType == ExpressionType.AndAlso)
                return new AutomationConditionExpression(new AndCondition(AsCondition(left), AsCondition(right)));

            if (node.NodeType == ExpressionType.Or || node.NodeType == ExpressionType.OrElse)
                return new AutomationConditionExpression(new OrCondition(AsCondition(left), AsCondition(right)));

            if (node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual)
            {
                PropertyCondition property = AsPropertyCondition(left, right) ?? AsPropertyCondition(right, left);

                if (property != null)
                {
                    if(node.NodeType == ExpressionType.NotEqual)
                        return new AutomationConditionExpression(new NotCondition(property));

                    return new AutomationConditionExpression(property);
                }
            }

            return Expression.MakeBinary(node.NodeType, left, right);
        }

        private PropertyCondition AsPropertyCondition(Expression exp, Expression value)
        {
            ConstantExpression ce = value as ConstantExpression;
            if(ce == null)
                return null;

            if (exp is AutomationPropertyExpression prop)
                return new PropertyCondition(prop.AutomationProperty, ce.Value);

            if (exp is AutomationPatternExpression pattern && ce.Value == null)
                return new PropertyCondition(GetCachedProperty(typeof(AutomationElement), "Is{0}Available".FormatWith(pattern.AutomationPattern.Name)), false);

            return null;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var expression = Visit(node.Expression);

            if (expression == parameter && (node.Member.Name == "Current" || node.Member.Name == "Cached"))
                return new AutomationCurrentExpression(node.Type);

            if (expression is AutomationCurrentExpression)
                return new AutomationPropertyExpression(GetCachedProperty(typeof(AutomationElement), node.Member.Name), node.Type);

            if (expression is AutomationPatternExpression pattern)
                return new AutomationPropertyExpression(GetCachedProperty(pattern.AutomationPattern, node.Member.Name), node.Type);

            return Expression.MakeMemberAccess(expression, node.Member);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.GetArgument("ae") == parameter && (node.Method.Name == "Pattern" || node.Method.Name == "TryPattern"))
                return new AutomationPatternExpression(node.Method.GetGenericArguments()[0], node.Type);

            return base.VisitMethodCall(node);
        }


        static readonly Dictionary<Type, Dictionary<string, AutomationProperty>> properties = new Dictionary<Type, Dictionary<string, AutomationProperty>>();
        static AutomationProperty GetCachedProperty(Type type, string propertyName)
        {
            var dic = properties.GetOrCreate(type, () =>
                  type.GetFields(BindingFlags.Static | BindingFlags.Public)
                  .Where(p => p.Name.EndsWith("Property"))
                  .ToDictionary(p => p.Name.RemoveEnd("Property".Length), p => (AutomationProperty)p.GetValue(null)));

            var result = dic.TryGetC(propertyName);

            if (result == null)
                throw new InvalidOperationException("Property {0} is not accessible on UIAutomation queries".FormatWith(propertyName));

            return result;
        }

        public ConditionBuilder()
        {
            // TODO: Complete member initialization
        }
    }
}
