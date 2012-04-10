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
        public CollectionElementType ElementType { get; private set; }

        internal CollectionElementToken(QueryToken parent, CollectionElementType type)
            : base(parent)
        {
            if (parent.Type.ElementType() == null)
                throw new InvalidOperationException("not a collection");

            this.ElementType = type;
        }

        public override Type Type
        {
            get { return Parent.Type.ElementType().Nullify().BuildLite(); }
        }

        public override string ToString()
        {
            return ElementType.NiceToString();
        }

        public override string Key
        {
            get { return ElementType.ToString(); }
        }



        protected override List<QueryToken> SubTokensInternal()
        {
            return SubTokensBase(Type, Implementations());
        }

        public override Implementations Implementations()
        {
            var pr = GetPropertyRoute();
            if (pr == null)
                return null;

            return pr.GetImplementations();
        }

        public override string Format
        {
            get { return Parent.Format; }
        }

        public override string Unit
        {
            get { return Parent.Unit; }
        }

        public override bool IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override bool HasAllOrAny()
        {
            return ElementType == CollectionElementType.All || ElementType == CollectionElementType.Any;
        }

        public override PropertyRoute GetPropertyRoute()
        {
            PropertyRoute parent = Parent.GetPropertyRoute();
            if (parent == null)
                return null;

            return parent.Add("Item");
        }

        public override string NiceName()
        {
            if (ElementType != CollectionElementType.Element)
                throw new InvalidOperationException("NiceName not supported for {0}".Formato(ElementType));

            Type parentElement = Parent.Type.ElementType().CleanType();

            if (parentElement.IsModifiableEntity())
                return parentElement.NiceName();
            
            return "Element of " + Parent.NiceName();
        }

        public override QueryToken Clone()
        {
            return new CollectionElementToken(Parent.Clone(), ElementType);
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
            MethodInfo mi = typeof(IQueryable).IsAssignableFrom(Parent.Type) ? (ElementType == CollectionElementType.All ? miAllQ : miAnyQ) :
                                                                               (ElementType == CollectionElementType.All ? miAllE : miAnyE);

            var collection = Parent.BuildExpression(context);
            
            return Expression.Call(mi.MakeGenericMethod(Parent.Type.ElementType()), collection, lambda);
        }

        internal ParameterExpression CreateParameter()
        {
            return Expression.Parameter(Parent.Type.ElementType());
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
                .Where(a => a.ElementType == CollectionElementType.Element)
                .Distinct()
                .OrderBy(a => a.FullKey().Length)
                .ToList();
        }

        public static string MultipliedMessage(List<CollectionElementToken> elements, Type entityType)
        {
            if (elements.IsEmpty())
                return null;

            return Resources.TheNumberOf0IsBeingMultipliedBy1.Formato(entityType.NiceName(), elements.CommaAnd(a => a.Parent.ToString()));
        }
    }



    [ForceLocalization]
    public enum CollectionElementType
    {
        Element,
        Any,
        All
    }   
}
