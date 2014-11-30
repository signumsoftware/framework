using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Signum.Utilities.Reflection;
using System.Collections;
using System.Reflection;
using Signum.Utilities;
using System.Runtime.Serialization;
using System.Globalization;
using System.ComponentModel;
using Signum.Entities.Basics;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class ResultColumn :ISerializable
    {
        Column column;
        public Column Column
        {
            get { return column; }
        }

        int index;
        public int Index
        {
            get { return index; }
            internal set { index = value; }
        }

        internal IList Values;

        public ResultColumn(Column column, IList values)
        {
            this.column = column;
            this.Values = values;
        }


        ResultColumn(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "column": column = (Column)entry.Value; break;
                    case "valuesList": Values = (IList)entry.Value; break;
                    case "valuesString": Values = Split((string)entry.Value, GetValueDeserializer()); break;
                }
            }
        }

        GenericInvoker<Func<int, IList>> listBuilder = new GenericInvoker<Func<int, IList>>(num => new List<int>(num));

        private IList Split(string concatenated, Func<string, object> deserialize)
        {
            string[] splitted = concatenated.Split('|');

            IList result = listBuilder.GetInvoker(column.Type)(splitted.Length);

            for (int i = 1; i < splitted.Length - 1; i++)
            {
                string str = splitted[i];
                if (string.IsNullOrEmpty(str))
                    result.Add(null);
                else
                    result.Add(deserialize(str));
            }

            return result;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("column", column);

            Func<object, string> serializer = GetValueSerializer();

            if (serializer == null)
                info.AddValue("valuesList", Values);
            else
            {
                string result = Join(Values, serializer);
                info.AddValue("valuesString", result);
            }
        }

        static string Join(IList values, Func<object, string> serializer)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('|');
            foreach (var item in values)
            {
                if (item != null)
                    sb.Append(serializer(item));

                sb.Append('|');
            }

            return sb.ToString();
        }

        Func<string, object> GetValueDeserializer()
        {
            if (column.Type.IsLite())
            {
                var type = column.Type.GetGenericArguments()[0];
                return str => DeserializeLite(Decode(str), type);
            }

            if (column.Type == typeof(string))
                return str => str == "&&" ? "&" :
                    str == "&" ? "" : 
                    Decode(str);

            var uType = column.Type.UnNullify();
            if (uType.IsEnum)
            {
                return str => Enum.ToObject(uType, int.Parse(str));
            }

            switch (Type.GetTypeCode(uType))
            {
                case TypeCode.Boolean: return str => str == "1";
                case TypeCode.Byte: return str => Byte.Parse(str, CultureInfo.InvariantCulture);
                case TypeCode.Decimal: return str => Decimal.Parse(str, CultureInfo.InvariantCulture);
                case TypeCode.Double: return str => Double.Parse(str, CultureInfo.InvariantCulture);
                case TypeCode.Int16: return str => Int16.Parse(str, CultureInfo.InvariantCulture);
                case TypeCode.Int32: return str => Int32.Parse(str, CultureInfo.InvariantCulture);
                case TypeCode.Int64: return str => Int64.Parse(str, CultureInfo.InvariantCulture);
                case TypeCode.SByte: return str => SByte.Parse(str, CultureInfo.InvariantCulture);
                case TypeCode.Single: return str => Single.Parse(str, CultureInfo.InvariantCulture);
                case TypeCode.UInt16: return str => UInt16.Parse(str, CultureInfo.InvariantCulture);
                case TypeCode.UInt32: return str => UInt32.Parse(str, CultureInfo.InvariantCulture);
                case TypeCode.UInt64: return str => UInt32.Parse(str, CultureInfo.InvariantCulture);
                case TypeCode.DateTime: return str => DateTime.ParseExact(str, "O", CultureInfo.InvariantCulture); 
            }

            throw new InvalidOperationException("Impossible to deserialize a ResultColumn of {0}".FormatWith(column.Type));
        }

        Func<object, string> GetValueSerializer()
        {
            if (column.Type.IsLite())
            {
                var type = column.Type.GetGenericArguments()[0];
                return obj => Encode(SerializeLite(obj, type));
            }
            
            if (column.Type == typeof(string))
                return obj =>
                {
                    string str = (string)obj;
                    return str == "&" ? "&&" :
                        str == "" ? "&" :
                        Encode(str);
                };

            if (column.Type.UnNullify().IsEnum)
                return obj => Convert.ChangeType(obj, typeof(int)).ToString();
            
            switch (Type.GetTypeCode(column.Type.UnNullify()))
            {
                case TypeCode.Boolean: return obj => ((bool)obj) ? "1" : "0";
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64: return obj => ((IFormattable)obj).ToString(null, CultureInfo.InvariantCulture);
                case TypeCode.DateTime: return obj => ((DateTime)obj).ToString("O", CultureInfo.InvariantCulture);
            }

            return null;
        }

        static string Encode(string p)
        {
            return p.Replace("{%%}", "{%%%}").Replace("|", "{%%}"); 
        }

        static string Decode(string p)
        {
            return p.Replace("{%%}", "|").Replace("{%%%}", "{%%}");
        }

        static string SerializeLite(object obj, Type defaultEntityType)
        {
            var lite = ((Lite<Entity>)obj);

            return lite.Id + ";" + (lite.EntityType == defaultEntityType ? null : TypeEntity.GetCleanName(lite.EntityType)) + ";" + lite.ToString();
        }

        static object DeserializeLite(string str, Type defaultEntityType)
        {
            string idStr = str.Before(';'); 

            string tmp = str.After(';');

            string typeStr = tmp.Before(';');

            string toStr = tmp.After(';');

            Type type = string.IsNullOrEmpty(typeStr) ? defaultEntityType : TypeEntity.TryGetType(typeStr);

            return Lite.Create(type, PrimaryKey.Parse(idStr, type), toStr);
        }
    }

    [Serializable]
    public class ResultTable
    {
        public ResultColumn entityColumn;
        public ColumnDescription EntityColumn
        {
            get { return entityColumn == null ? null : ((ColumnToken)entityColumn.Column.Token).Column; }
        }

        public bool HasEntities
        {
            get { return entityColumn != null; }
        }

        ResultColumn[] columns;
        public ResultColumn[] Columns { get { return columns; } }

        [NonSerialized]
        ResultRow[] rows;
        public ResultRow[] Rows { get { return rows; } }

        public ResultTable(ResultColumn[] columns, int? totalElements, Pagination pagination)
        {
            this.entityColumn = columns.Where(c => c.Column is _EntityColumn).SingleOrDefaultEx();
            this.columns = columns.Where(c => !(c.Column is _EntityColumn) && c.Column.Token.IsAllowed() == null).ToArray();

            CreateIndices(columns);

            this.totalElements = totalElements;
            this.pagination = pagination;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            CreateIndices(columns);
        }

        void CreateIndices(ResultColumn[] columns)
        {
            int rows = columns.Select(a => a.Values.Count).Distinct().SingleEx(() => "Count");

            for (int i = 0; i < Columns.Length; i++)
                Columns[i].Index = i;

            this.rows = 0.To(rows).Select(i => new ResultRow(i, this)).ToArray();
        }

        public DataTable ToDataTable()
        {
            DataTable dt = new DataTable("Table");
            dt.Columns.AddRange(Columns.Select(c => new DataColumn(c.Column.Name,
                c.Column.Type.IsLite() ? typeof(string) : c.Column.Type.UnNullify())).ToArray());
            foreach (var row in Rows)
            {
                dt.Rows.Add(Columns.Select((_, i) => Convert(row[i])).ToArray());
            }
            return dt;
        }

        private object Convert(object p)
        {
            if (p is Lite<Entity>)
                return ((Lite<Entity>)p).KeyLong();

            return p;
        }

        int? totalElements;
        public int? TotalElements { get { return totalElements; } }

        Pagination pagination; 
        public Pagination Pagination { get { return pagination; } }

        public int? TotalPages
        {
            get { return Pagination is Pagination.Paginate ? ((Pagination.Paginate)Pagination).TotalPages(TotalElements.Value) : (int?)null; } 
        }

        public int? StartElementIndex
        {
            get { return Pagination is Pagination.Paginate ? ((Pagination.Paginate)Pagination).StartElementIndex() : (int?)null; }
        }

        public int? EndElementIndex
        {
            get { return Pagination is Pagination.Paginate ? ((Pagination.Paginate)Pagination).EndElementIndex(Rows.Count()) : (int?)null; }
        }
    }


    [Serializable]
    public class ResultRow : INotifyPropertyChanged
    {
        public readonly int Index;
        public readonly ResultTable Table;

        bool isDirty; 
        public bool IsDirty
        {
            get { return isDirty; }
            set
            {
                isDirty = value;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDirty"));
            }
        }

        public object this[int columnIndex]
        {
            get { return Table.Columns[columnIndex].Values[Index]; }
        }

        public object this[ResultColumn column]
        {
            get { return column.Values[Index]; }
        }

        internal ResultRow(int index, ResultTable table)
        {
            this.Index = index;
            this.Table = table;
        }

        public Lite<Entity> Entity
        {
            get { return (Lite<Entity>)Table.entityColumn.Values[Index]; }
        }

        public T GetValue<T>(string columnName)
        {
            return (T)this[Table.Columns.Where(c => c.Column.Name == columnName).SingleEx(() => columnName)];
        }

        public T GetValue<T>(int columnIndex)
        {
            return (T)this[columnIndex];
        }

        public T GetValue<T>(ResultColumn column)
        {
            return (T)this[column];
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
