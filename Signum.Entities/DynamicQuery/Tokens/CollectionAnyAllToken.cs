using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class CollectionAnyAllToken : QueryToken
    {
        public CollectionAnyAllType CollectionAnyAllType { get; private set; }

        Type elementType;
        internal CollectionAnyAllToken(QueryToken parent, CollectionAnyAllType type)
            : base(parent)
        {
            elementType = parent.Type.ElementType();
            if (elementType == null)
                throw new InvalidOperationException("not a collection");

            this.CollectionAnyAllType = type;
        }

        public override Type Type
        {
            get { return elementType.BuildLiteNullifyUnwrapPrimaryKey(new[] { this.GetPropertyRoute() }); }
        }

        public override string ToString()
        {
            return CollectionAnyAllType.NiceToString();
        }

        public override string Key
        {
            get { return CollectionAnyAllType.ToString(); }
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

        public override bool HasAllOrAny() => true;

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
            return null;
        }

        public override QueryToken Clone()
        {
            return new CollectionAnyAllToken(Parent.Clone(), this.CollectionAnyAllType);
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

        public override string TypeColor
        {
            get { return "#0000FF"; }
        }

        static MethodInfo miAnyE = ReflectionTools.GetMethodInfo((IEnumerable<string> col) => col.Any(null)).GetGenericMethodDefinition();
        static MethodInfo miAllE = ReflectionTools.GetMethodInfo((IEnumerable<string> col) => col.All(null)).GetGenericMethodDefinition();
        static MethodInfo miAnyQ = ReflectionTools.GetMethodInfo((IQueryable<string> col) => col.Any(null)).GetGenericMethodDefinition();
        static MethodInfo miAllQ = ReflectionTools.GetMethodInfo((IQueryable<string> col) => col.All(null)).GetGenericMethodDefinition();

        public Expression BuildAnyAll(Expression collection, ParameterExpression param, Expression body)
        {
            if (this.CollectionAnyAllType == CollectionAnyAllType.AnyNo)
                body = Expression.Not(body);

            var lambda = Expression.Lambda(body, param);
            
            MethodInfo mi = typeof(IQueryable).IsAssignableFrom(collection.Type) ?
                 (this.CollectionAnyAllType == CollectionAnyAllType.All ? miAllQ : miAnyQ) :
                 (this.CollectionAnyAllType == CollectionAnyAllType.All ? miAllE : miAnyE);

            var result = Expression.Call(mi.MakeGenericMethod(param.Type), collection, lambda);

            if (this.CollectionAnyAllType == CollectionAnyAllType.NoOne)
                return Expression.Not(result);

            return result;
        }
    }


    [DescriptionOptions(DescriptionOptions.Members)]
    public enum CollectionAnyAllType
    {
        Any,
        All,
        NoOne,
        AnyNo,
    }
}
