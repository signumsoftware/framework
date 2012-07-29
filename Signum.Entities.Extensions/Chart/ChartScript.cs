using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Files;

namespace Signum.Entities.Chart
{
    [Serializable]
    public class ChartScriptDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        Lite<FileDN> icon;
        public Lite<FileDN> Icon
        {
            get { return icon; }
            set { Set(ref icon, value, () => Icon); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string script;
        [StringLengthValidator(AllowNulls = false, Min = 3)]
        public string Script
        {
            get { return script; }
            set { Set(ref script, value, () => Script); }
        }

        GroupByChart groupBy;
        public GroupByChart GroupBy
        {
            get { return groupBy; }
            set { Set(ref groupBy, value, () => GroupBy); }
        }

        MList<ChartScriptColumnDN> columns = new MList<ChartScriptColumnDN>();
        public MList<ChartScriptColumnDN> Columns
        {
            get { return columns; }
            set { Set(ref columns, value, () => Columns); }
        }

        static Expression<Func<ChartScriptDN, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public string ColumnsToString()
        {
            return Columns.ToString(a => a.ColumnType.ToString(), "|");
        }

        protected override void PreSaving(ref bool graphModified)
        {
            Columns.ForEach((c, i) => c.Index = i);

            base.PreSaving(ref graphModified);
        }

        protected override void PostRetrieving()
        {
            Columns.Sort(c => c.Index);
            
            base.PostRetrieving();
        }
    }

    public enum GroupByChart
    {
        Always,
        Optional,
        Never
    }

    [Serializable]
    public class ChartScriptColumnDN : EmbeddedEntity       
    {
        int index;
        public int Index
        {
            get { return index; }
            set { Set(ref index, value, () => Index); }
        }

        [NotNullable, SqlDbType(Size = 80)]
        string propertyName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 80)]
        public string PropertyName
        {
            get { return propertyName; }
            set { Set(ref propertyName, value, () => PropertyName); }
        }

        bool isOptional;
        public bool IsOptional
        {
            get { return isOptional; }
            set { Set(ref isOptional, value, () => IsOptional); }
        }
     
        ChartColumnType columnType;
        public ChartColumnType ColumnType
        {
            get { return columnType; }
            set { Set(ref columnType, value, () => ColumnType); }
        }

        bool isGroupColor;
        public bool IsGroupColor
        {
            get { return isGroupColor; }
            set { Set(ref isGroupColor, value, () => IsGroupColor); }
        }

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Is(() => IsGroupColor) && IsGroupColor && ColumnType != ChartColumnType.Groupable)
            {
                return "{0} can not be set if column type is not {1}".Formato(pi.NiceName(), ColumnType.NiceToString());
            }

            return base.PropertyValidation(pi);
        }

        public bool IsGroupKey
        {
            get { return columnType == ChartColumnType.Groupable || columnType == ChartColumnType.GroupableAndPositionable; }
        }
    }

    public enum ChartColumnType
    {
        Groupable, // String | Entity | Boolean | Enum | Integer | Date | Interval,
        Magnitude, //Integer | Decimal
        Positionable, //Integer | Decimal | Date | DateTime
        GroupableAndPositionable, // Integer | Date 
    }
}
