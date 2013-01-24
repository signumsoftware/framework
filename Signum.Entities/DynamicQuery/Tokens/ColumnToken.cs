using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Linq.Expressions;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class ColumnToken : QueryToken
    {
        public ColumnDescription Column { get; private set; }

        internal ColumnToken(ColumnDescription column)
            : base(null)
        {
            if (column == null)
                throw new ArgumentNullException("column");

            this.Column = column;
        }

        public override string Key
        {
            get { return Column.Name; }
        }

        public override string ToString()
        {
            return Column.ToString();
        }

        public override Type Type
        {
            get { return Column.Type; }
        }

        public override string Format
        {
            get { return Column.Format; }
        }

        public override string Unit
        {
            get { return Column.Unit; }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            throw new InvalidOperationException("BuildExpressionInternal not supported for ColumnToken");
        }

        protected override List<QueryToken> SubTokensInternal()
        {
            if (Column.Type.UnNullify() == typeof(DateTime))
            {
                if (Column.PropertyRoutes != null)
                {
                    DateTimePrecision? precission =
                        Column.PropertyRoutes.Select(pr => Validator.TryGetPropertyValidator(pr.Parent.Type, pr.PropertyInfo.Name)
                        .Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefaultEx())
                        .Select(dtp => dtp.TryCS(d => d.Precision)).Distinct().Only();

                    if (precission != null)
                        return DateTimeProperties(this, precission.Value);
                }

                if (Column.Format == "d")
                    return DateTimeProperties(this, DateTimePrecision.Days);
            }

            return SubTokensBase(Column.Type, Column.Implementations);
        }

        public override Implementations? GetImplementations()
        {
            return Column.Implementations;
        }

        public override string IsAllowed()
        {
            return null;  //If it wasn't, sould be filtered before
        }

        public override PropertyRoute GetPropertyRoute()
        {
            if (Column.PropertyRoutes != null)
                return Column.PropertyRoutes[0]; //HACK: compatibility with IU entitiy elements

            Type type = Lite.Extract(Type);
            if (type != null && typeof(IdentifiableEntity).IsAssignableFrom(type))
                return PropertyRoute.Root(type);

            return null;
        }

        public override string NiceName()
        {
            return Column.DisplayName;
        }

        public override QueryToken Clone()
        {
            return new ColumnToken(Column);
        }

        public override string TypeColor
        {
            get
            {
                if (Column.IsEntity)
                    return "#0000FF";

                return base.TypeColor;
            }
        }
    }
}
