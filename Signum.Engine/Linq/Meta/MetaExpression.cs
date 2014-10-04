using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.ObjectModel;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;

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
            return "MetaProjector({0})".Formato(this.Projector.ToString());
        }  
    }

    internal class MetaExpression : MetaBaseExpression
    {
        public bool IsEntity
        {
            get { return typeof(ModifiableEntity).IsAssignableFrom(Type); }
        }

        public readonly Meta Meta;

        public MetaExpression(Type type, Meta meta):
            base(MetaExpressionType.MetaExpression, type)
        {
            this.Meta = meta;
        }

        public override string ToString()
        {
            return "Exp({0})".Formato(Meta);
        }

        internal static MetaExpression FromRoute(Type type, Implementations? implementations, PropertyRoute pr)
        {
            if (pr == null)
                return new MetaExpression(type.UnNullify().CleanType(), new DirtyMeta(implementations, new Meta[0]));

            return new MetaExpression(type.UnNullify().CleanType(), new CleanMeta(implementations, new[] { pr }));
        }
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
            return "ExpMList({0}, {1})".Formato(Parent, Element);
        }
    }
}
