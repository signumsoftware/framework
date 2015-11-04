using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class Column
    {
        string displayName;
        public string DisplayName
        {
            get { return displayName; }
            set { displayName = value; }
        }

        QueryToken token;
        public QueryToken Token { get { return token; } }

        public bool IsVisible = true;

        public Column(QueryToken token, string displayName)
        {
            this.token = token;
            this.displayName = displayName;
        }

        public Column(ColumnDescription cd, object queryName)
            : this(new ColumnToken(cd, queryName), cd.DisplayName)
        {
        }

        public string Name { get { return Token.FullKey(); } }
        public virtual Type Type { get { return Token.Type; } }
        public Implementations? Implementations { get { return Token.GetImplementations(); } }
        public string Format { get { return Token.Format; } }
        public string Unit { get { return Token.Unit; } }

        public override string ToString()
        {
            return "{0} '{1}'".FormatWith(Token.FullKey(), DisplayName);
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

    [Serializable]
    internal class _EntityColumn : Column
    {
        public _EntityColumn(ColumnDescription entityColumn, object queryName)
            : base(new ColumnToken(entityColumn, queryName), null)
        {
            if (!entityColumn.IsEntity)
                throw new ArgumentException("entityColumn");
        }
    }

    [InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
    public enum ColumnOptionsMode
    {
        Add,
        Remove,
        Replace,
    }
}
