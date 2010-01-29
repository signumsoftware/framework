using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Data;
using System.Reflection;
using System.Diagnostics;
using Signum.Entities.Reflection;
using Signum.Utilities.DataStructures;
using System.Collections;
using System.Linq.Expressions;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class QueryDescription
    {
        public List<StaticColumn> StaticColumns { get; set; }
    }

    [Serializable]
    public abstract class Column
    {
        public int Index { get; internal set; }

        public string Name { get; internal set; }
        public Type Type { get; internal set; }

        public string Format { get; set; }
        public string Unit { get; set; }
        public Implementations Implementations { get; set; }

        public string DisplayName { get; set; }

        public bool Filterable { get; set; }
        public bool Visible { get; set; }
        public bool Sortable { get; set; }

        public abstract bool IsAllowed();

        public override string ToString()
        {
            return "{0} {1}".Formato(Type.TypeName(), Name);
        }
    }

    [Serializable]
    public class UserColumn : Column
    {
        public int UserQueryIndex { get; set; }
        public QueryToken Token { get; internal set; }

        public UserColumn(int index, QueryToken token)
        {

        }

        public override bool IsAllowed()
        {
            return Token.IsAllowed();
        }
    }

    [Serializable]
    public class StaticColumn : Column
    {
        public PropertyInfo twinProperty;
        public PropertyInfo TwinProperty
        {
            get { return twinProperty; }
            set
            {
                twinProperty = value;
                if (twinProperty != null)
                {
                    DisplayName = twinProperty.NiceName();
                    Format = Reflector.FormatString(twinProperty);
                    Unit = twinProperty.SingleAttribute<UnitAttribute>().TryCC(u => u.UnitName);
                }
            }
        }

        [NonSerialized]
        readonly internal Delegate Getter;
        [NonSerialized]
        readonly internal Meta Meta;

        public const string Entity = "Entity";
        public bool IsEntity
        {
            get { return this.Name == Entity; }
        }

        public StaticColumn(int index, MemberInfo mi, Meta meta, Delegate getter)
        {
            Index = index;
            Name = mi.Name;
            Getter = getter;

            Type = mi.ReturningType();
            Meta = meta;

            if (typeof(IdentifiableEntity).IsInstanceOfType(Type))
                throw new InvalidOperationException("{0} column returns subtype of IdentifiableEntity, use a Lite instead!!".Formato(mi.MemberName()));

            if (meta is CleanMeta && ((CleanMeta)meta).PropertyPath.PropertyRouteType == PropertyRouteType.Property)
                TwinProperty = ((CleanMeta)meta).PropertyPath.PropertyInfo;
            else if (mi is PropertyInfo)
                DisplayName = ((PropertyInfo)mi).NiceName();
            else
                DisplayName = mi.Name.NiceName();

            Sortable = true;
            if (IsEntity)
            {
                Visible = false;
                Filterable = false;
            }
            else
            {
                Filterable = true;
                Visible = true;
            }

        }

        public override bool IsAllowed()
        {
            return Meta == null || Meta.IsAllowed();
        }
    }
}