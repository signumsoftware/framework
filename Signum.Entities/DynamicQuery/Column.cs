using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class Column : IEquatable<Column>
    {
        public string DisplayName { get; set; }
        public QueryToken Token { get; private set; }

        public Column(QueryToken token, string displayName)
        {
            Token = token;
            DisplayName = displayName;
        }

        public Column(ColumnDescription cd)
            : this(QueryToken.NewColumn(cd), cd.DisplayName)
        {
        }

        public string Name { get { return Token.FullKey(); } }
        public virtual Type Type { get { return Token.Type; } }
        public Implementations? Implementations { get { return Token.GetImplementations(); } }
        public string Format { get { return Token.Format; } }
        public string Unit { get { return Token.Unit; } }

        public override string ToString()
        {
            return "{0} '{1}'".Formato(Token.FullKey(), DisplayName);
        }

        public bool Equals(Column other)
        {
            return Token.Equals(other.Token) &&
                DisplayName == other.DisplayName;
        }

        public override bool Equals(object obj)
        {
            return obj is Column && base.Equals((Column)obj);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }

    //Temporaly used by the engine
    public class _EntityColumn : Column
    {
        public _EntityColumn(ColumnDescription entityColumn)
            : base(QueryToken.NewColumn(entityColumn), null)
        {
            if (!entityColumn.IsEntity)
                throw new ArgumentException("entityColumn");
        }
    }

    public enum ColumnOptionsMode
    {
        Add,
        Remove,
        Replace,
    }
}
