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
        public Implementations Implementations { get; set; }

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
                            Implementations = AggregateImplementations(PropertyRoutes, Type.CleanType());
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

            //if (Type.IsIIdentifiable())
            //    throw new InvalidOperationException("The Type of column {0} is a subtype of IIdentifiable, use a Lite instead".Formato(mi.MemberName()));

            if (IsEntity && !Type.CleanType().IsIIdentifiable())
                throw new InvalidOperationException("Entity must be a Lite or an IIdentifiable");

            if (meta is CleanMeta && ((CleanMeta)meta).PropertyRoutes.All(pr => pr.PropertyRouteType != PropertyRouteType.Root))
            {
                PropertyRoutes = ((CleanMeta)meta).PropertyRoutes;
            }
        }

        internal static Implementations AggregateImplementations(PropertyRoute[] routes, Type cleanType)
        {
            return AggregateImplementations(routes.Select(a => a.GetImplementations() ??
                new ImplementedByAttribute(a.Type.CleanType())).NotNull(), cleanType);
        }

        private static Implementations AggregateImplementations(IEnumerable<Implementations> collection, Type cleanType)
        {
            if (collection.IsEmpty())
                return null;

            var only = collection.Only();
            if (only != null)
            {
                ImplementedByAttribute ib = only as ImplementedByAttribute;
                if (ib != null && ib.ImplementedTypes.Length == 1 && ib.ImplementedTypes[0] == cleanType)
                    return null;

                return only;
            }
            ImplementedByAttribute iba = (ImplementedByAttribute)collection.FirstOrDefault(a => a.IsByAll);
            if (iba != null)
                return iba;

            var types = collection
                .Cast<ImplementedByAttribute>()
                .SelectMany(ib => ib.ImplementedTypes)
                .Distinct()
                .ToArray();

            if (types.Length == 1 && types[0] == cleanType)
                return null;
         
            return new ImplementedByAttribute(types);
        }

        public string DisplayName()
        {
            if (OverrideDisplayName != null)
                return OverrideDisplayName();

            if (IsEntity)
                return this.Type.CleanType().NiceName();

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
            return new ColumnDescription(Name, Reflector.IsIIdentifiable(Type) ? Reflector.GenerateLite(Type) : Type)
            {
                PropertyRoutes = propertyRoutes,
                Implementations = Implementations,

                DisplayName = DisplayName(),
                Format = Format,
                Unit = Unit,
            };
        }

        public Type DefaultEntityType()
        {
            if (Implementations == null)
                return this.Type.CleanType();

            if (Implementations.IsByAll)
                return null;

            return ((ImplementedByAttribute)Implementations).ImplementedTypes.FirstOrDefault();
        }

        public bool CompatibleWith(Type entityType)
        {
            if (Implementations == null)
                return Type.CleanType() == entityType;

            if (Implementations.IsByAll)
                return true;

            return ((ImplementedByAttribute)Implementations).ImplementedTypes.Contains(entityType);
        }
    }
}
