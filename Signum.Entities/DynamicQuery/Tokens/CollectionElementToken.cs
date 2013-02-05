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
using Signum.Entities.Properties;

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
            get { return elementType.Nullify().BuildLite(); }
        }

        public override string ToString()
        {
            return CollectionElementType.NiceToString();
        }

        public override string Key
        {
            get { return CollectionElementType.ToString(); }
        }

        protected override List<QueryToken> SubTokensInternal()
        {
            return SubTokensBase(Type, GetImplementations());
        }

        public override Implementations? GetImplementations()
        {
            return Parent.GetElementImplementations();
        }

        public override string Format
        {
            get
            {
                ExtensionToken et = Parent as ExtensionToken;

                if (et != null && et.IsProjection)
                    return et.ElementFormat;

                return Parent.Format;
            }
        }

        public override string Unit
        {
            get
            {
                ExtensionToken et = Parent as ExtensionToken;

                if (et != null && et.IsProjection)
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
            return CollectionElementType == CollectionElementType.All || CollectionElementType == CollectionElementType.Any;
        }

        public override PropertyRoute GetPropertyRoute()
        {
            ExtensionToken et = Parent as ExtensionToken;
            if (et != null && et.IsProjection)
                return et.GetElementPropertyRoute();

            PropertyRoute parent = Parent.GetPropertyRoute();
            if (parent != null && parent.Type.ElementType() != null)
               return parent.Add("Item");

            return parent; 
        }

        public override string NiceName()
        {
            if (CollectionElementType != CollectionElementType.Element)
                throw new InvalidOperationException("NiceName not supported for {0}".Formato(CollectionElementType));

            Type parentElement = elementType.CleanType();

            if (parentElement.IsModifiableEntity())
                return parentElement.NiceName();
            
            return "Element of " + Parent.NiceName();
        }

        public override QueryToken Clone()
        {
            return new CollectionElementToken(Parent.Clone(), CollectionElementType);
        }

        static MethodInfo miAnyE = ReflectionTools.GetMethodInfo((IEnumerable<string> col) => col.Any(null)).GetGenericMethodDefinition();
        static MethodInfo miAllE = ReflectionTools.GetMethodInfo((IEnumerable<string> col) => col.All(null)).GetGenericMethodDefinition();
        static MethodInfo miAnyQ = ReflectionTools.GetMethodInfo((IQueryable<string> col) => col.Any(null)).GetGenericMethodDefinition();
        static MethodInfo miAllQ = ReflectionTools.GetMethodInfo((IQueryable<string> col) => col.All(null)).GetGenericMethodDefinition();

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            throw new InvalidOperationException("CollectionElementToken does not support this method");
        }

        internal Expression BuildExpressionLambda(BuildExpressionContext context, LambdaExpression lambda)
        {
            MethodInfo mi = typeof(IQueryable).IsAssignableFrom(Parent.Type) ? 
                    (CollectionElementType == CollectionElementType.All ? miAllQ : miAnyQ) :
                    (CollectionElementType == CollectionElementType.All ? miAllE : miAnyE);

            var collection = Parent.BuildExpression(context);
            
            return Expression.Call(mi.MakeGenericMethod(elementType), collection, lambda);
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
                .SelectMany(t => t.FollowC(tt => tt.Parent))
                .OfType<CollectionElementToken>()
                .Where(a => a.CollectionElementType == CollectionElementType.Element)
                .Distinct()
                .OrderBy(a => a.FullKey().Length)
                .ToList();
        }

        public static string MultipliedMessage(List<CollectionElementToken> elements, Type entityType)
        {
            if (elements.IsEmpty())
                return null;

            return ValidationMessage.TheNumberOf0IsBeingMultipliedBy1.NiceToString().Formato(entityType.NiceName(), elements.CommaAnd(a => a.Parent.ToString()));
        }

        public override string TypeColor
        {
            get { return "#0000FF"; }
        }
    }



    [DescriptionOptions(DescriptionOptions.Members)]
    public enum CollectionElementType
    {
        Element,
        Any,
        All
    }   
}
