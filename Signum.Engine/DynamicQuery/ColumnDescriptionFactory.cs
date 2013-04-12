using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Engine.Maps;

namespace Signum.Engine.DynamicQuery
{
    public class ColumnDescriptionFactory
    {
        readonly internal Meta Meta;
        public Func<string> OverrideDisplayName { get; set; }
        public Func<string> OverrideIsAllowed { get; set; }

        public string Name { get; internal set; }
        public Type Type { get; internal set; }

        public string Format { get; set; }
        public string Unit { get; set; }
        Implementations? implementations;
        public Implementations? Implementations
        {
            get { return implementations; }

            set
            {
                if (value != null && !value.Value.IsByAll)
                {
                    var ct = Type.CleanType();
                    string errors = value.Value.Types.Where(t => !ct.IsAssignableFrom(t)).ToString(a => a.Name, ", ");

                    if (errors.Any())
                        throw new InvalidOperationException("Column {0} Implenentations should be assignable to {1}: {2}".Formato(Name, ct.Name, errors));
                }

                implementations = value;
            }
        }

        PropertyRoute[] propertyRoutes;
        public PropertyRoute[] PropertyRoutes
        {
            get { return propertyRoutes; }
            set
            {
                propertyRoutes = value;
                if (propertyRoutes != null && propertyRoutes.Any() /*Out of IB casting*/)
                {
                    var cleanType = Type.CleanType();

                    Implementations = GetImplementations(propertyRoutes, cleanType);
                    Format = GetFormat(propertyRoutes);
                    Unit = GetUnit(propertyRoutes);
                }
            }
        }

        internal static Entities.Implementations? GetImplementations(PropertyRoute[] propertyRoutes, Type cleanType)
        {
            if (!cleanType.IsIIdentifiable())
                return (Implementations?)null;

            var only = propertyRoutes.Only();
            if (only != null && only.PropertyRouteType == PropertyRouteType.Root)
                return Signum.Entities.Implementations.By(cleanType);

            var aggregate = AggregateImplementations(propertyRoutes.Select(pr => pr.GetImplementations()));

            if (!cleanType.IsAssignableFrom(propertyRoutes.First().Type.CleanType()))
                return CastImplementations(aggregate, cleanType);

            return aggregate;
        }

        internal static string GetUnit(PropertyRoute[] routes)
        {
            switch (routes[0].PropertyRouteType)
            {
                case PropertyRouteType.LiteEntity:
                case PropertyRouteType.Root:
                    return null;
                case PropertyRouteType.FieldOrProperty:
                    return routes.Select(pr => pr.SimplifyNoRoot().PropertyInfo.SingleAttribute<UnitAttribute>().TryCC(u => u.UnitName)).Distinct().Only();
                case PropertyRouteType.MListItems:
                    return null;
            }

            throw new InvalidOperationException();
        }

        internal static string GetFormat(PropertyRoute[] routes)
        {
            switch (routes[0].PropertyRouteType)
            {
                case PropertyRouteType.LiteEntity:
                case PropertyRouteType.Root:
                    return null;
                case PropertyRouteType.FieldOrProperty:
                    return routes.Select(pr => Reflector.FormatString(pr)).Distinct().Only();
                case PropertyRouteType.MListItems:
                    return Reflector.FormatString(routes[0].Type);
            }

            throw new InvalidOperationException();
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

            if (meta is CleanMeta)
            {
                PropertyRoutes = ((CleanMeta)meta).PropertyRoutes;
            }
        }

        public static Implementations AggregateImplementations(IEnumerable<Implementations> implementations)
        {
            if (implementations.IsEmpty())
                throw new InvalidOperationException("implementations is Empty");

            if (implementations.Count() == 1)
                return implementations.First();

            if (implementations.Any(a => a.IsByAll))
                return Signum.Entities.Implementations.ByAll;

            var types = implementations
                .SelectMany(ib => ib.Types)
                .Distinct()
                .ToArray();

            return Signum.Entities.Implementations.By(types);
        }

        internal static Implementations CastImplementations(Implementations implementations, Type cleanType)
        {
            if (implementations.IsByAll)
            {
                

                if (!Schema.Current.Tables.ContainsKey(cleanType))
                    throw new InvalidOperationException("Tye type {0} is not registered in the schema as a concrete table".Formato(cleanType));

                return Signum.Entities.Implementations.By(cleanType);
            }

            if (implementations.Types.All(cleanType.IsAssignableFrom))
                return implementations;

            return Signum.Entities.Implementations.By(implementations.Types.Where(cleanType.IsAssignableFrom).ToArray());
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

        public string IsAllowed()
        {
            if (OverrideIsAllowed != null)
                return OverrideIsAllowed();

            if (Meta != null)
                return Meta.IsAllowed();

            return null;
        }

        public ColumnDescription BuildColumnDescription()
        {
            return new ColumnDescription(Name, Reflector.IsIIdentifiable(Type) ? Lite.Generate(Type) : Type)
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
