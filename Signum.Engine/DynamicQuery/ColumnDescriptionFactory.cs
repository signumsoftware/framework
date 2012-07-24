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
using Signum.Engine.Maps;

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
                if (propertyRoutes != null && propertyRoutes.Any() /*Out of IB casting*/)
                {
                    var cleanType = Type.CleanType();
                    var only = propertyRoutes.Only();

                    Implementations = !cleanType.IsIIdentifiable() ? (Implementations?)null :
                        only != null && only.PropertyRouteType == PropertyRouteType.Root ? Signum.Entities.Implementations.By(cleanType) :
                        CastImplementations(AggregateImplementations(PropertyRoutes.Select(pr => pr.GetImplementations())), cleanType);

                    switch (propertyRoutes[0].PropertyRouteType)
                    {
                        case PropertyRouteType.LiteEntity:
                        case PropertyRouteType.Root:
                            return;
                        case PropertyRouteType.FieldOrProperty:
                            Format = GetFormat(propertyRoutes);
                            Unit = GetUnit(propertyRoutes);
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
            return value.Select(pr => pr.SimplifyNoRoot().PropertyInfo.SingleAttribute<UnitAttribute>().TryCC(u => u.UnitName)).Distinct().Only();
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
