using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using Signum.Entities.Reflection;
using System.ComponentModel;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class CollectionElementToken : QueryToken
    {
        public CollectionElementType CollectionElementType { get; private set; }

        Type elementType;
        internal CollectionElementToken(QueryToken parent, CollectionElementType type)
            : base(parent)
        {
            elementType = parent.Type.ElementType();
            if (elementType == null)
                throw new InvalidOperationException("not a collection");

            this.CollectionElementType = type;
        }

        public override Type Type
        {
            get { return elementType.BuildLiteNullifyUnwrapPrimaryKey(new[] { this.GetPropertyRoute() }); }
        }

        public override string ToString()
        {
            return CollectionElementType.NiceToString();
        }

        public override string Key
        {
            get { return CollectionElementType.ToString(); }
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            return SubTokensBase(Type, options, GetImplementations());
        }

        public override Implementations? GetImplementations()
        {
            return Parent.GetElementImplementations();
        }

        public override string Format
        {
            get
            {

                if (Parent is ExtensionToken et && et.IsProjection)
                    return et.ElementFormat;

                return Parent.Format;
            }
        }

        public override string Unit
        {
            get
            {

                if (Parent is ExtensionToken et && et.IsProjection)
                    return et.ElementUnit;

                return Parent.Unit;
            }
        }

        public override string IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override bool HasAllOrAny()
        {
            return 
                CollectionElementType != CollectionElementType.Element &&
                CollectionElementType != CollectionElementType.Element2 &&
                CollectionElementType != CollectionElementType.Element3;
        }

        public override bool HasElement()
        {
            return base.HasElement() ||
                CollectionElementType == CollectionElementType.Element ||
                CollectionElementType == CollectionElementType.Element2 ||
                CollectionElementType == CollectionElementType.Element3;
        }

        public override PropertyRoute GetPropertyRoute()
        {
            if (Parent is ExtensionToken et && et.IsProjection)
                return et.GetElementPropertyRoute();

            PropertyRoute parent = Parent.GetPropertyRoute();
            if (parent != null && parent.Type.ElementType() != null)
                return parent.Add("Item");

            return parent;
        }

        public override string NiceName()
        {
            if (!CollectionElementType.IsElement())
                return null;

            Type parentElement = elementType.CleanType();

            if (parentElement.IsModifiableEntity())
                return parentElement.NiceName();

            return "Element of " + Parent.NiceName();
        }

        public override QueryToken Clone()
        {
            return new CollectionElementToken(Parent.Clone(), CollectionElementType);
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            throw new InvalidOperationException("CollectionElementToken should have a replacement at this stage");
        }


        internal ParameterExpression CreateParameter()
        {
            return Expression.Parameter(elementType);
        }

        internal Expression CreateExpression(ParameterExpression parameter)
        {
            return parameter.BuildLite().Nullify();
        }

        public static List<CollectionElementToken> GetElements(HashSet<QueryToken> allTokens)
        {
            return allTokens
                .SelectMany(t => t.Follow(tt => tt.Parent))
                .OfType<CollectionElementToken>()
                .Where(a => a.CollectionElementType.IsElement())
                .Distinct()
                .OrderBy(a => a.FullKey().Length)
                .ToList();
        }

        public static string MultipliedMessage(List<CollectionElementToken> elements, Type entityType)
        {
            if (elements.IsEmpty())
                return null;

            return ValidationMessage.TheNumberOf0IsBeingMultipliedBy1.NiceToString().FormatWith(entityType.NiceName(), elements.CommaAnd(a => a.Parent.ToString()));
        }

        public override string TypeColor
        {
            get { return "#0000FF"; }
        }
    }


    public class FilterBuildExpressionContext : BuildExpressionContext
    {
        public FilterBuildExpressionContext(BuildExpressionContext context)
            : base(context.TupleType, context.Parameter, context.Replacemens.ToDictionary())
        {
        }

        public readonly Dictionary<CollectionElementToken, AnyAllFilter> AllAnyFilters = new Dictionary<CollectionElementToken, AnyAllFilter>();
        public readonly List<IFilterExpression> Filters = new List<IFilterExpression>();
    }

    public interface IFilterExpression
    {
        Expression ToExpression(FilterBuildExpressionContext ctx);
    }

    public class FilterExpression : IFilterExpression
    {
        public FilterExpression(Expression expresion)
        {
            this.Expression = expresion;
        }

        public readonly Expression Expression;

        public Expression ToExpression(FilterBuildExpressionContext ctx)
        {
            return Expression;
        }
    }

    public class AnyAllFilter : IFilterExpression
    {
        public AnyAllFilter(CollectionElementToken ce)
        {
            if (ce.CollectionElementType.ToString().StartsWith("Element"))
                throw new ArgumentException("ce should be non-Free Any or All");

            this.Token = ce;
            this.Parameter = ce.CreateParameter();
        }

        public readonly ParameterExpression Parameter;
        public readonly CollectionElementToken Token;
        public readonly List<IFilterExpression> Filters = new List<IFilterExpression>();

        static MethodInfo miAnyE = ReflectionTools.GetMethodInfo((IEnumerable<string> col) => col.Any(null)).GetGenericMethodDefinition();
        static MethodInfo miAllE = ReflectionTools.GetMethodInfo((IEnumerable<string> col) => col.All(null)).GetGenericMethodDefinition();
        static MethodInfo miAnyQ = ReflectionTools.GetMethodInfo((IQueryable<string> col) => col.Any(null)).GetGenericMethodDefinition();
        static MethodInfo miAllQ = ReflectionTools.GetMethodInfo((IQueryable<string> col) => col.All(null)).GetGenericMethodDefinition();

        public Expression ToExpression(FilterBuildExpressionContext ctx)
        {
            var collection = Token.Parent.BuildExpression(ctx);

            var and = Filters.Select(f => f.ToExpression(ctx)).AggregateAnd();

            string type = Token.CollectionElementType.ToString();

            if (type.StartsWith("AnyNo"))
                and = Expression.Not(and);

            var lambda = Expression.Lambda(and, Parameter);

            MethodInfo mi = typeof(IQueryable).IsAssignableFrom(Token.Parent.Type) ?
                 (type.StartsWith("All") ? miAllQ : miAnyQ) :
                 (type.StartsWith("All") ? miAllE : miAnyE);

            var result = Expression.Call(mi.MakeGenericMethod(Parameter.Type), collection, lambda);

            if (type.StartsWith("NoOne"))
                return Expression.Not(result);

            return result;
        }
    }

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum CollectionElementType
    {
        Element,
        Any,
        All,
        NoOne,
        AnyNo,

        [Description("Element (2)")]
        Element2,
        [Description("Any (2)")]
        Any2,
        [Description("All (2)")]
        All2,
        [Description("No one (2)")]
        NoOne2,
        [Description("Any no (2)")]
        AnyNo2,

        [Description("Element (3)")]
        Element3,
        [Description("Any (3)")]
        Any3,
        [Description("All (3)")]
        All3,
        [Description("No one (3)")]
        NoOne3,
        [Description("Any no (3)")]
        AnyNo3,
    }

    public static class CollectionElementTypeExtensions
    {
        public static bool IsElement(this CollectionElementType cet)
        {
            return cet == CollectionElementType.Element ||
                cet == CollectionElementType.Element2 ||
                cet == CollectionElementType.Element3;
        }
    }
}
