using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Properties;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System.Reflection;
using System.Linq.Expressions;

namespace Signum.Engine.DynamicQuery
{
    public class ColumnDescriptionFactory
    {
        readonly internal Meta Meta;
        public Func<string> OverrideDisplayName { get; set; }
        public Func<bool> OverrideIsAllowed { get; set; }

        public string Name { get; internal set; }
        public Type Type { get; internal set; }

        public string Format { get; set; }
        public string Unit { get; set; }
        public Implementations? Implementations { get; set; }

        PropertyRoute[] propertyRoutes;
        public PropertyRoute[] PropertyRoutes
        {
            get { return propertyRoutes; }
            set
            {
                propertyRoutes = value;
                if (propertyRoutes != null)
                {
                    switch (propertyRoutes[0].PropertyRouteType)
                    {
                        case PropertyRouteType.LiteEntity:
                        case PropertyRouteType.Root:
                            throw new InvalidOperationException("PropertyRoute can not be of RouteType Root");
                        case PropertyRouteType.FieldOrProperty:
                            Format = GetFormat(propertyRoutes);
                            Unit = GetUnit(propertyRoutes);

                            var cleanType = Type.CleanType();
                            Implementations = cleanType.IsIIdentifiable() ? (Implementations?)AggregateImplementations(PropertyRoutes.Select(pr=>pr.GetImplementations()), cleanType) : null;
                            return;
                        case PropertyRouteType.MListItems:
                            Format = Reflector.FormatString(propertyRoutes[0].Type);
                            return;
                    }
                }
            }
        }

        internal static string GetUnit(PropertyRoute[] value)
        {
            return value.Select(pr => pr.PropertyInfo.SingleAttribute<UnitAttribute>().TryCC(u => u.UnitName)).Distinct().Only();
        }

        internal static string GetFormat(PropertyRoute[] value)
        {
            return value.Select(pr => Reflector.FormatString(pr)).Distinct().Only();
        }

        public ColumnDescriptionFactory(int index, MemberInfo mi, Meta meta)
        {
            Name = mi.Name;

            Type = mi.ReturningType();
            Meta = meta;

            if (Type.IsIIdentifiable())
                throw new InvalidOperationException("The Type of column {0} is a subtype of IIdentifiable, use a Lite instead".Formato(mi.MemberName()));

            Type cleanType = Reflector.ExtractLite(Type);
            if (IsEntity && cleanType == null)
                throw new InvalidOperationException("Entity must be a Lite");

            if (meta is CleanMeta && ((CleanMeta)meta).PropertyRoutes.All(pr => pr.PropertyRouteType != PropertyRouteType.Root))
            {
                PropertyRoutes = ((CleanMeta)meta).PropertyRoutes;
            }
        }

        public static Implementations AggregateImplementations(IEnumerable<Implementations> collection, Type cleanType)
        {
            if (collection.IsEmpty())
                return Signum.Entities.Implementations.By(cleanType.CleanType());
            

            if (collection.Count() == 1)
                return collection.First();

            if (collection.Any(a => a.IsByAll))
                return Signum.Entities.Implementations.ByAll;

            var types = collection
                .SelectMany(ib => ib.Types)
                .Distinct()
                .ToArray();

            return Signum.Entities.Implementations.By(types);
        }

        public string DisplayName()
        {
            if (OverrideDisplayName != null)
                return OverrideDisplayName();

            if (IsEntity)
                return this.Type.NiceName();

            if (propertyRoutes != null && 
                propertyRoutes[0].PropertyRouteType == PropertyRouteType.FieldOrProperty &&
                propertyRoutes[0].PropertyInfo.Name == Name)
            {
                var result = propertyRoutes.Select(pr=>pr.PropertyInfo.NiceName()).Only();
                if (result != null)
                    return result;
            }

            return Name.NiceName();
        }

        public void SetPropertyRoutes<T>(params Expression<Func<T, object>>[] expression)
            where T : IdentifiableEntity
        {
            PropertyRoutes = expression.Select(exp => PropertyRoute.Construct(exp)).ToArray();
        }

        public bool IsEntity
        {
            get { return this.Name == ColumnDescription.Entity; }
        }

        public bool IsAllowed()
        {
            if (OverrideIsAllowed != null)
                return OverrideIsAllowed();

            if (Meta != null)
                return Meta.IsAllowed();

            return true;
        }

        public ColumnDescription BuildColumnDescription()
        {
            return new ColumnDescription(Name, Type)
            {
                PropertyRoutes = propertyRoutes,
                Implementations = Implementations,

                DisplayName = DisplayName(),
                Format = Format,
                Unit = Unit,
            };
        }
    }
}
