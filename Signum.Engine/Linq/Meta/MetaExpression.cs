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

    internal class MetaProjectorExpression : Expression
    {
        public readonly Expression Projector;

        readonly Type type;
        public override Type Type
        {
            get { return type; }
        }

        public override ExpressionType NodeType
        {
            get { return (ExpressionType)MetaExpressionType.MetaProjector; }
        }

        public MetaProjectorExpression(Type type, Expression projector)
        {
            this.type = type;
            this.Projector = projector;
        }
    }

    internal class MetaExpression : Expression
    {
        public bool IsEntity
        {
            get { return typeof(ModifiableEntity).IsAssignableFrom(Type); }
        }

        readonly Type type;
        public override Type Type
        {
            get { return type; }
        }

        public override ExpressionType NodeType
        {
            get { return (ExpressionType)MetaExpressionType.MetaExpression; }
        }

        public readonly Meta Meta;

        public MetaExpression(Type type, Meta meta)
        {
            this.type = type;
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

    internal class MetaMListExpression : Expression
    {
        readonly Type type;
        public override Type Type
        {
            get { return type; }
        }

        public override ExpressionType NodeType
        {
            get { return (ExpressionType)MetaExpressionType.MetaMListExpression; }
        }

        public readonly CleanMeta Parent;
        public readonly CleanMeta Element;

        public MetaMListExpression(Type type, CleanMeta parent, CleanMeta element)
        {
            this.type = type;
            this.Parent = parent;
            this.Element = element;
        }


        public override string ToString()
        {
            return "ExpMList({0}, {1})".Formato(Parent, Element);
        }
    }
}
