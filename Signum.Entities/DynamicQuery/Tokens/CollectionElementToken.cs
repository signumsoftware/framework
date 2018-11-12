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

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum CollectionElementType
    {
        Element,
        [Description("Element (2)")]
        Element2,
        [Description("Element (3)")]
        Element3,
    }

}
