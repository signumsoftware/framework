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
using Signum.Entities.Properties;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class QueryDescription
    {
        public object QueryName { get; set; }
        public List<StaticColumn> StaticColumns { get; set; }
    }


    [Serializable]
    public abstract class Column
    {
        public abstract int Index { get; }

        public string Name { get; internal set; }
        public Type Type { get; internal set; }

        public string Format { get; set; }
        public string Unit { get; set; }
        public Implementations Implementations { get; set; }

        public string DisplayName{get;set;}

        public abstract bool Filterable { get; set; }
        public abstract bool Visible { get; set; }
        public abstract bool Sortable { get; set; }

        public override string ToString()
        {
            return "{0} {1}".Formato(Type.TypeName(), Name);
        }

        public abstract QueryToken GetQueryToken();

        public abstract bool IsAllowed();
    }

    [Serializable]
    public class UserColumn : Column
    {
        public int UserColumnIndex { get; set; }
        public QueryToken Token { get; internal set; }

        int baseIndex;
        public UserColumn(int baseIndex, QueryToken token)
        {
            this.baseIndex = baseIndex;
            this.Token = token;
            this.Name = token.FullKey();
            this.Implementations = token.Implementations();
            this.Format = token.Format;
            this.Unit = token.Unit;
            this.Type = token.Type;
        }

        public override QueryToken GetQueryToken()
        {
            return Token;
        }

        public override bool Visible
        {
            get { return true;  }
            set { throw new InvalidOperationException(); }
        }

        public override bool Filterable
        {
            get { return false; }
            set { throw new InvalidOperationException(); }

        }

        public override bool Sortable
        {
            get { return true; }
            set { throw new InvalidOperationException(); }
        }

        public override int Index
        {
            get { return baseIndex + UserColumnIndex; }
        }

        public override bool IsAllowed()
        {
            return Token.IsAllowed();
        }
    }

    [Serializable]
    public class StaticColumn : Column
    {
        int index;
        public override int Index
        {
            get { return index; }
        }

        public StaticColumn(int index, string name, Type type)
        {
            this.index = index;
            this.Name = name;
            this.Type = type;
        }

        public override bool Visible { get; set; }
        public override bool Filterable { get; set; }
        public override bool Sortable { get; set; }

        public bool Allowed { get; set; }

        public PropertyRoute PropertyRoute { get; set; }

        public const string Entity = "Entity";
        public bool IsEntity
        {
            get { return this.Name == Entity; }
        }

        public override QueryToken GetQueryToken()
        {
            return QueryToken.NewColumn(this);
        }

        public override bool IsAllowed()
        {
            return Allowed;
        }
    }
}