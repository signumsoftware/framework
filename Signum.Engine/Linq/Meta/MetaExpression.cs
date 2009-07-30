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
    }

    internal class MetaProjectorExpression : Expression
    {
        public readonly Expression Projector;

        public MetaProjectorExpression(Type type, Expression projector)
            : base((ExpressionType)MetaExpressionType.MetaProjector, type)
        {
            this.Projector = projector;
        }
    }

    internal class MetaExpression : Expression
    {
        public bool IsEntity
        {
            get
            {
                return typeof(ModifiableEntity).IsAssignableFrom(Type);
            }
        }

        public readonly Meta Meta;

        public MetaExpression(Type type, Meta meta)
            : base((ExpressionType)MetaExpressionType.MetaExpression, type)
        {
            this.Meta = meta;
        }
    }   
}
