using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Properties;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.DynamicQuery
{

    [Serializable]
    public class AsTypeToken : QueryToken
    {
        Type entityType;
        internal AsTypeToken(QueryToken parent, Type type)
            : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            if (type == null)
                throw new ArgumentNullException("type");

            this.entityType = type;
        }

        public override Type Type
        {
            get { return BuildLite(entityType); }
        }

        public override string ToString()
        {
            return Resources.As0.Formato(Type.NiceName());
        }

        public override string Key
        {
            get { return "({0})".Formato(entityType.FullName.Replace(".", ":")); }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            Expression baseExpression = Parent.BuildExpression(context);

            Expression result = Expression.TypeAs(ExtractEntity(baseExpression, false), entityType);

            return BuildLite(result);
        }

        protected override List<QueryToken> SubTokensInternal()
        {
            return SubTokensBase(entityType, null);
        }

        public override string Format
        {
            get { return null; }
        }

        public override string Unit
        {
            get { return null; }
        }

        public override Implementations? GetImplementations()
        {
            return Implementations.By(entityType);
        }

        public override bool IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return PropertyRoute.Root(entityType);
        }

        public override string NiceName()
        {
            return Resources._0As1.Formato(Parent.ToString(), entityType.NiceName());
        }

        public override QueryToken Clone()
        {
            return new AsTypeToken(Parent.Clone(), entityType);
        }
    }
    
}
