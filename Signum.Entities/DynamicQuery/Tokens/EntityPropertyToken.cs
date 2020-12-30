using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Utilities;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class EntityPropertyToken : QueryToken
    {
        public PropertyInfo PropertyInfo { get; private set; }

        public PropertyRoute PropertyRoute { get; private set; }

        static readonly PropertyInfo piId = ReflectionTools.GetPropertyInfo((Entity e) => e.Id);

        public static QueryToken IdProperty(QueryToken parent)
        {
            return new EntityPropertyToken(parent, piId, PropertyRoute.Root(parent.Type.CleanType()).Add(piId)) { Priority = 10 };
        }

        QueryToken parent;
        public override QueryToken? Parent => parent;
       
       internal EntityPropertyToken(QueryToken parent, PropertyInfo pi, PropertyRoute pr)
        {
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
            this.PropertyInfo = pi ?? throw new ArgumentNullException(nameof(pi));
            this.PropertyRoute = pr;
        }

        public override Type Type
        {
            get { return PropertyInfo.PropertyType.BuildLiteNullifyUnwrapPrimaryKey(new[] { this.GetPropertyRoute()! }); }
        }

        public override string ToString()
        {
            return PropertyInfo.NiceName();
        }

        public override string Key
        {
            get { return PropertyInfo.Name; }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var baseExpression = parent.BuildExpression(context);

            if (PropertyInfo.Name == nameof(Entity.Id) ||
                PropertyInfo.Name == nameof(Entity.ToStringProperty))
            {
                var entityExpression = baseExpression.ExtractEntity(true);

                return Expression.Property(entityExpression, PropertyInfo.Name).BuildLiteNullifyUnwrapPrimaryKey(new[] { this.PropertyRoute }); // Late binding over Lite or Identifiable
            }
            else
            {
                var entityExpression = baseExpression.ExtractEntity(false);

                if (PropertyRoute.Parent != null && PropertyRoute.Parent.PropertyRouteType == PropertyRouteType.Mixin)
                    entityExpression = Expression.Call(entityExpression, MixinDeclarations.miMixin.MakeGenericMethod(PropertyRoute.Parent.Type));

                Expression result = Expression.Property(entityExpression, PropertyInfo);

                return result.BuildLiteNullifyUnwrapPrimaryKey(new[] { this.PropertyRoute });
            }
        }

        protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
        {
            var type = this.Type;
            var uType = type.UnNullify();

            if (uType == typeof(DateTime) || uType == typeof(DateTimeOffset))
            {
                PropertyRoute? route = this.GetPropertyRoute();

                if (route != null)
                {
                    var att = Validator.TryGetPropertyValidator(route.Parent!.Type, route.PropertyInfo!.Name)?.Validators.OfType<DateTimePrecisionValidatorAttribute>().SingleOrDefaultEx();
                    if (att != null)
                    {
                        return DateTimeProperties(this, att.Precision).AndHasValue(this);
                    }
                }
            }

            if (uType == typeof(TimeSpan))
            {
                PropertyRoute? route = this.GetPropertyRoute();

                if (route != null)
                {
                    var att = Validator.TryGetPropertyValidator(route.Parent!.Type, route.PropertyInfo!.Name)?.Validators.OfType<TimeSpanPrecisionValidatorAttribute>().SingleOrDefaultEx();
                    if (att != null)
                    {
                        return TimeSpanProperties(this, att.Precision).AndHasValue(this);
                    }
                }
            }

            if (uType == typeof(double) ||
                uType == typeof(float) ||
                uType == typeof(decimal))
            {
                PropertyRoute? route = this.GetPropertyRoute();

                if (route != null)
                {
                    var att = Validator.TryGetPropertyValidator(route.Parent!.Type, route.PropertyInfo!.Name)?.Validators
                        .OfType<DecimalsValidatorAttribute>().SingleOrDefaultEx();
                    if (att != null)
                    {
                        return StepTokens(this, att.DecimalPlaces).AndHasValue(this);
                    }

                    var format = Reflector.FormatString(route);
                    if (format != null)
                        return StepTokens(this, Reflector.NumDecimals(format)).AndHasValue(this);
                }
            }

            return SubTokensBase(this.Type, options, GetImplementations());
        }

        public override Implementations? GetImplementations()
        {
            return GetPropertyRoute()!.TryGetImplementations();
        }

        public override string? Format
        {
            get { return Reflector.FormatString(this.GetPropertyRoute()!); }
        }

        public override string? Unit
        {
            get { return PropertyInfo.GetCustomAttribute<UnitAttribute>()?.UnitName; }
        }

        public override string? IsAllowed()
        {
            string? parent = this.parent.IsAllowed();

            string? route = GetPropertyRoute()?.IsAllowed();

            if (parent.HasText() && route.HasText())
                return QueryTokenMessage.And.NiceToString().Combine(parent!, route!);

            return parent ?? route;
        }

        public override PropertyRoute? GetPropertyRoute()
        {
            return PropertyRoute;
        }

        public override string NiceName()
        {
            return PropertyInfo.NiceName();
        }

        public override QueryToken Clone()
        {
            return new EntityPropertyToken(parent.Clone(), PropertyInfo, PropertyRoute);
        }
    }
}
