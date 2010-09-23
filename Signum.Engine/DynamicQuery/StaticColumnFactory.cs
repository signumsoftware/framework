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
    public class StaticColumnFactory
    {
        readonly internal Delegate Getter;
        readonly internal Meta Meta;
        public Func<string> OverrideDisplayName { get; set; }

        public int Index { get; private set; }

        public string Name { get; internal set; }
        public Type Type { get; internal set; }

        public string Format { get; set; }
        public string Unit { get; set; }
        public Implementations Implementations { get; set; }


        public bool Filterable { get; set; }
        public bool Visible { get; set; }
        public bool Sortable { get; set; }


        PropertyRoute propertyRoute;
        public PropertyRoute PropertyRoute
        {
            get { return propertyRoute; }
            set
            {
                propertyRoute = value;
                if (propertyRoute != null)
                {
                    switch (propertyRoute.PropertyRouteType)
                    {
                        case PropertyRouteType.LiteEntity:
                        case PropertyRouteType.Root:
                            throw new InvalidOperationException(Resources.PropertyRouteCanNotBeOfTypeRoot);
                        case PropertyRouteType.Property:
                            PropertyInfo pi = propertyRoute.PropertyInfo;
                            Format = Reflector.FormatString(propertyRoute);
                            Unit = pi.SingleAttribute<UnitAttribute>().TryCC(u => u.UnitName);
                            return;
                        case PropertyRouteType.MListItems:
                            Format = Reflector.FormatString(propertyRoute.Type);
                            return;
                    }
                }
            }
        }

        public StaticColumnFactory(int index, MemberInfo mi, Meta meta, Delegate getter)
        {
            this.Index = index;
            Name = mi.Name;
            Getter = getter;

            Type = mi.ReturningType();
            Meta = meta;

            if (typeof(IIdentifiable).IsAssignableFrom(Type))
                throw new InvalidOperationException("The Type of column {0} is a subtype of IIdentifiable, use a Lite instead".Formato(mi.MemberName()));

            Type cleanType = Reflector.ExtractLite(Type);
            if (IsEntity && cleanType == null)
                throw new InvalidOperationException("Entity must be a Lite");

            if (meta is CleanMeta && ((CleanMeta)meta).PropertyRoute.PropertyRouteType != PropertyRouteType.Root)
            {
                PropertyRoute = ((CleanMeta)meta).PropertyRoute;
                Implementations = PropertyRoute.GetImplementations();
            }

            Sortable = true;
            Filterable = true;
            Visible = !IsEntity;
        }

        protected string DisplayName()
        {
            if (OverrideDisplayName != null)
                return OverrideDisplayName();

            if (IsEntity)
                return this.Type.NiceName();

            if (PropertyRoute != null && propertyRoute.PropertyRouteType == PropertyRouteType.Property && PropertyRoute.PropertyInfo.Name == Name)
                return propertyRoute.PropertyInfo.NiceName();

            return Name.NiceName();
        }

        public void SetPropertyRoute<T>(Expression<Func<T, object>> expression)
            where T : IdentifiableEntity
        {
            PropertyRoute = PropertyRoute.Construct(expression);
        }

        public bool IsEntity
        {
            get { return this.Name == StaticColumn.Entity; }
        }

        public bool IsAllowed()
        {
            return Meta == null || Meta.IsAllowed();
        }

        public StaticColumn BuildStaticColumn()
        {
            return new StaticColumn(Index, Name, Type)
            {
                PropertyRoute = propertyRoute,
                Implementations = Implementations,

                Visible = Visible,
                Filterable = Filterable,
                Sortable = Sortable,

                DisplayName = DisplayName(),
                Format = Format,
                Unit = Unit,

                Allowed = Meta == null || Meta.IsAllowed()
            };
        }

        public Type DefaultEntityType()
        {
            if (Implementations == null)
                return Reflector.ExtractLite(this.Type);

            if (Implementations.IsByAll)
                return null;

            return ((ImplementedByAttribute)Implementations).ImplementedTypes.FirstOrDefault();
        }

        public bool CompatibleWith(Type entityType)
        {
            if (Implementations == null)
                return Reflector.ExtractLite(this.Type) == entityType;

            if (Implementations.IsByAll)
                return true;

            return ((ImplementedByAttribute)Implementations).ImplementedTypes.Contains(entityType);
        }
    }
}
