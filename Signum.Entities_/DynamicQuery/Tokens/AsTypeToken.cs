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
        Type type;
        internal AsTypeToken(QueryToken parent, Type type)
            : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            if (type == null)
                throw new ArgumentNullException("type");

            this.type = type;
        }

        public override Type Type
        {
            get { return BuildLite(type); }
        }

        public override string ToString()
        {
            return Resources.As0.Formato(Type.NiceName());
        }

        public override string Key
        {
            get { return "({0})".Formato(type.FullName.Replace(".", ":")); }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            Expression baseExpression = Parent.BuildExpression(context);

            Expression result = Expression.TypeAs(ExtractEntity(baseExpression, false), type);

            return BuildLite(result);
        }

        protected override QueryToken[] SubTokensInternal()
        {
            return SubTokensBase(type, null);
        }

        public override string Format
        {
            get { return null; }
        }

        public override string Unit
        {
            get { return null; }
        }

        public override Implementations Implementations()
        {
            return null;
        }

        public override bool IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return PropertyRoute.Root(type);
        }

        public override string NiceName()
        {
            return Resources._0As1.Formato(Parent.ToString(), type.NiceName());
        }

        public override QueryToken Clone()
        {
            return new AsTypeToken(Parent.Clone(), Type);
        }
    }
    
}
