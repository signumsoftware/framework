using System;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Linq
{
    internal enum MetaExpressionType
    {
        MetaProjector = 2000,
        MetaExpression,
        MetaMListExpression,
        MetaConstant
    }

    internal abstract class MetaBaseExpression : Expression
    {
        readonly Type type;
        public override Type Type
        {
            get { return type; }
        }

        readonly MetaExpressionType metaNodeType;
        public MetaExpressionType MetaNodeType
        {
            get { return metaNodeType; }
        }

        public override ExpressionType NodeType
        {
            get { return ExpressionType.Extension; }
        }

        protected MetaBaseExpression(MetaExpressionType nodeType, Type type)
        {
            this.type = type;
            this.metaNodeType = nodeType;
        }

        public abstract override string ToString();
    }

    internal class MetaProjectorExpression : MetaBaseExpression
    {
        public readonly Expression Projector;

        public MetaProjectorExpression(Type type, Expression projector)
            : base(MetaExpressionType.MetaProjector, type)
        {
            this.Projector = projector;
        }

        public override string ToString()
        {
            return "MetaProjector({0})".FormatWith(this.Projector.ToString());
        }
    }

    internal class MetaExpression : MetaBaseExpression
    {
        public bool IsEntity
        {
            get { return typeof(ModifiableEntity).IsAssignableFrom(Type); }
        }

        public Meta Meta { get; private set; }

        public MetaExpression(Type type, Meta meta):
            base(MetaExpressionType.MetaExpression, type)
        {
            this.Meta = meta;
        }

        public override string ToString()
        {
            return "Exp({0})".FormatWith(Meta);
        }

        internal static MetaExpression FromToken(QueryToken token, Type sourceType)
        {
            var pr = token.GetPropertyRoute();

            if (pr == null)
                return new MetaExpression(sourceType, new DirtyMeta(token.GetImplementations(), Array.Empty<Meta>()));

            if (!sourceType.IsLite()  && pr.Type.IsLite())
                return new MetaExpression(sourceType, new CleanMeta(token.GetImplementations(), new[] { pr.Add("Entity") }));

            return new MetaExpression(sourceType, new CleanMeta(token.GetImplementations(), new[] { pr }));

            //throw new InvalidOperationException("Impossible to convert {0} to {1}".FormatWith(pr.Type.TypeName(), sourceType.TypeName()));
        }

        static readonly MethodInfo miToLite = ReflectionTools.GetMethodInfo((Entity e) => e.ToLite());
    }

    internal class MetaMListExpression : MetaBaseExpression
    {
        public readonly CleanMeta Parent;
        public readonly CleanMeta Element;

        public MetaMListExpression(Type type, CleanMeta parent, CleanMeta element)
            :base(MetaExpressionType.MetaMListExpression, type)
        {
            this.Parent = parent;
            this.Element = element;
        }


        public override string ToString()
        {
            return "ExpMList({0}, {1})".FormatWith(Parent, Element);
        }
    }
}
