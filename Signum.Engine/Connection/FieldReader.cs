using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities.Reflection;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using Signum.Engine.Maps;
using Signum.Entities;
using System.Data.SqlTypes;
using System.Data.Common;
//using Microsoft.SqlServer.Types;
using Signum.Utilities.ExpressionTrees;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;
using System.IO;
using Npgsql;

namespace Signum.Engine
{
    public class FieldReader
    {
        DbDataReader reader;
        TypeCode[] typeCodes;

        private const TypeCode tcGuid = (TypeCode)20;
        private const TypeCode tcTimeSpan = (TypeCode)21;
        private const TypeCode tcDateTimeOffset = (TypeCode)22;
        private const TypeCode tcNpgsqlDate = (TypeCode)24;

        public int? LastOrdinal;
        public string? LastMethodName;

        TypeCode GetTypeCode(int ordinal)
        {
            Type type = reader.GetFieldType(ordinal);
            TypeCode tc = Type.GetTypeCode(type);
            if (tc == TypeCode.Object)
            {
                if (type == typeof(Guid))
                    tc = tcGuid;

                if (type == typeof(TimeSpan))
                    tc = tcTimeSpan;

                if (type == typeof(DateTimeOffset))
                    tc = tcDateTimeOffset;

                if (type == typeof(NpgsqlTypes.NpgsqlDate))
                    tc = tcNpgsqlDate;
            }
            return tc;
        }

        bool isPostgres;

        public FieldReader(DbDataReader reader)
        {
            this.isPostgres = Schema.Current.Settings.IsPostgres;
            this.reader = reader;

            this.typeCodes = new TypeCode[reader.FieldCount];
            for (int i = 0; i < typeCodes.Length; i++)
                typeCodes[i] = GetTypeCode(i);
        }

        public bool IsNull(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(IsNull);
            return reader.IsDBNull(ordinal);
        }

        public string? GetString(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetString);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            switch (typeCodes[ordinal])
            {
                case TypeCode.Byte:
                    return reader.GetByte(ordinal).ToString();
                case TypeCode.Int16:
                    return reader.GetInt16(ordinal).ToString();
                case TypeCode.Int32:
                    return reader.GetInt32(ordinal).ToString();
                case TypeCode.Int64:
                    return reader.GetInt64(ordinal).ToString();
                case TypeCode.Double:
                    return reader.GetDouble(ordinal).ToString();
                case TypeCode.Single:
                    return reader.GetFloat(ordinal).ToString();
                case TypeCode.Decimal:
                    return reader.GetDecimal(ordinal).ToString();
                case TypeCode.DateTime:
                    return reader.GetDateTime(ordinal).ToString();
                case tcGuid:
                    return reader.GetGuid(ordinal).ToString();
                case TypeCode.String:
                    return reader.GetString(ordinal);
                default:
                    return reader.GetValue(ordinal).ToString();
            }
        }

        public byte[]? GetByteArray(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetByteArray);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return (byte[])reader.GetValue(ordinal);
        }

        public bool GetBoolean(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetBoolean);
            switch (typeCodes[ordinal])
            {
                case TypeCode.Boolean:
                    return reader.GetBoolean(ordinal);
                case TypeCode.Byte:
                    return reader.GetByte(ordinal) != 0;
                case TypeCode.Int16:
                    return reader.GetInt16(ordinal) != 0;
                case TypeCode.Int32:
                    return reader.GetInt32(ordinal) != 0;
                case TypeCode.Int64:
                    return reader.GetInt64(ordinal) != 0;
                case TypeCode.String:
                    return bool.Parse(reader.GetString(ordinal));
                default:
                    return ReflectionTools.ChangeType<bool>(reader.GetValue(ordinal));
            }
        }

        public bool? GetNullableBoolean(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableBoolean);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetBoolean(ordinal);
        }


        public Byte GetByte(int ordinal)
        {
            LastOrdinal = ordinal;
            switch (typeCodes[ordinal])
            {
                case TypeCode.Byte:
                    return reader.GetByte(ordinal);
                case TypeCode.Int16:
                    return (Byte)reader.GetInt16(ordinal);
                case TypeCode.Int32:
                    return (Byte)reader.GetInt32(ordinal);
                case TypeCode.Int64:
                    return (Byte)reader.GetInt64(ordinal);
                case TypeCode.Double:
                    return (Byte)reader.GetDouble(ordinal);
                case TypeCode.Single:
                    return (Byte)reader.GetFloat(ordinal);
                case TypeCode.Decimal:
                    return (Byte)reader.GetDecimal(ordinal);
                default:
                    return ReflectionTools.ChangeType<Byte>(reader.GetValue(ordinal));
            }
        }

        public Byte? GetNullableByte(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableByte);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetByte(ordinal);
        }


        public Char GetChar(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetChar);
            switch (typeCodes[ordinal])
            {
                case TypeCode.Byte:
                    return (Char)reader.GetByte(ordinal);
                case TypeCode.Int16:
                    return (Char)reader.GetInt16(ordinal);
                case TypeCode.Int32:
                    return (Char)reader.GetInt32(ordinal);
                case TypeCode.Int64:
                    return (Char)reader.GetInt64(ordinal);
                case TypeCode.Double:
                    return (Char)reader.GetDouble(ordinal);
                case TypeCode.Single:
                    return (Char)reader.GetFloat(ordinal);
                case TypeCode.Decimal:
                    return (Char)reader.GetDecimal(ordinal);
                default:
                    return ReflectionTools.ChangeType<Char>(reader.GetValue(ordinal));
            }
        }

        public Char? GetNullableChar(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableChar);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetChar(ordinal);
        }


        public Single GetFloat(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetFloat);
            switch (typeCodes[ordinal])
            {
                case TypeCode.Byte:
                    return (Single)reader.GetByte(ordinal);
                case TypeCode.Int16:
                    return (Single)reader.GetInt16(ordinal);
                case TypeCode.Int32:
                    return (Single)reader.GetInt32(ordinal);
                case TypeCode.Int64:
                    return (Single)reader.GetInt64(ordinal);
                case TypeCode.Double:
                    return (Single)reader.GetDouble(ordinal);
                case TypeCode.Single:
                    return reader.GetFloat(ordinal);
                case TypeCode.Decimal:
                    return (Single)reader.GetDecimal(ordinal);
                default:
                    return ReflectionTools.ChangeType<Single>(reader.GetValue(ordinal));
            }
        }

        public Single? GetNullableFloat(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableFloat);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetFloat(ordinal);
        }


        public Double GetDouble(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetDouble);
            switch (typeCodes[ordinal])
            {
                case TypeCode.Byte:
                    return (Double)reader.GetByte(ordinal);
                case TypeCode.Int16:
                    return (Double)reader.GetInt16(ordinal);
                case TypeCode.Int32:
                    return (Double)reader.GetInt32(ordinal);
                case TypeCode.Int64:
                    return (Double)reader.GetInt64(ordinal);
                case TypeCode.Double:
                    return reader.GetDouble(ordinal);
                case TypeCode.Single:
                    return (Double)reader.GetFloat(ordinal);
                case TypeCode.Decimal:
                    return (Double)reader.GetDecimal(ordinal);
                default:
                    return ReflectionTools.ChangeType<Double>(reader.GetValue(ordinal));
            }
        }

        public Double? GetNullableDouble(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableDouble);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetDouble(ordinal);
        }


        public Decimal GetDecimal(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetDecimal);
            switch (typeCodes[ordinal])
            {
                case TypeCode.Byte:
                    return (Decimal)reader.GetByte(ordinal);
                case TypeCode.Int16:
                    return (Decimal)reader.GetInt16(ordinal);
                case TypeCode.Int32:
                    return (Decimal)reader.GetInt32(ordinal);
                case TypeCode.Int64:
                    return (Decimal)reader.GetInt64(ordinal);
                case TypeCode.Double:
                    return (Decimal)reader.GetDouble(ordinal);
                case TypeCode.Single:
                    return (Decimal)reader.GetFloat(ordinal);
                case TypeCode.Decimal:
                    return reader.GetDecimal(ordinal);
                default:
                    return ReflectionTools.ChangeType<Decimal>(reader.GetValue(ordinal));
            }
        }

        public Decimal? GetNullableDecimal(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableDecimal);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetDecimal(ordinal);
        }


        public Int16 GetInt16(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetInt16);
            switch (typeCodes[ordinal])
            {
                case TypeCode.Byte:
                    return (Int16)reader.GetByte(ordinal);
                case TypeCode.Int16:
                    return reader.GetInt16(ordinal);
                case TypeCode.Int32:
                    return (Int16)reader.GetInt32(ordinal);
                case TypeCode.Int64:
                    return (Int16)reader.GetInt64(ordinal);
                case TypeCode.Double:
                    return (Int16)reader.GetDouble(ordinal);
                case TypeCode.Single:
                    return (Int16)reader.GetFloat(ordinal);
                case TypeCode.Decimal:
                    return (Int16)reader.GetDecimal(ordinal);
                default:
                    return ReflectionTools.ChangeType<Int16>(reader.GetValue(ordinal));
            }
        }

        public Int16? GetNullableInt16(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableInt16);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetInt16(ordinal);
        }


        public Int32 GetInt32(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetInt32);
            switch (typeCodes[ordinal])
            {
                case TypeCode.Byte:
                    return (Int32)reader.GetByte(ordinal);
                case TypeCode.Int16:
                    return (Int32)reader.GetInt16(ordinal);
                case TypeCode.Int32:
                    return reader.GetInt32(ordinal);
                case TypeCode.Int64:
                    return (Int32)reader.GetInt64(ordinal);
                case TypeCode.Double:
                    return (Int32)reader.GetDouble(ordinal);
                case TypeCode.Single:
                    return (Int32)reader.GetFloat(ordinal);
                case TypeCode.Decimal:
                    return (Int32)reader.GetDecimal(ordinal);
                default:
                    return ReflectionTools.ChangeType<Int32>(reader.GetValue(ordinal));
            }
        }

        public Int32? GetNullableInt32(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableInt32);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetInt32(ordinal);
        }


        public Int64 GetInt64(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetInt64);
            switch (typeCodes[ordinal])
            {
                case TypeCode.Byte:
                    return (Int64)reader.GetByte(ordinal);
                case TypeCode.Int16:
                    return (Int64)reader.GetInt16(ordinal);
                case TypeCode.Int32:
                    return (Int64)reader.GetInt32(ordinal);
                case TypeCode.Int64:
                    return (Int64)reader.GetInt64(ordinal);
                case TypeCode.Double:
                    return (Int64)reader.GetDouble(ordinal);
                case TypeCode.Single:
                    return (Int64)reader.GetFloat(ordinal);
                case TypeCode.Decimal:
                    return (Int64)reader.GetDecimal(ordinal);
                default:
                    return ReflectionTools.ChangeType<Int64>(reader.GetValue(ordinal));
            }
        }

        public Int64? GetNullableInt64(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableInt64);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetInt64(ordinal);
        }


        public DateTime GetDateTime(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetDateTime);
            DateTime dt;
            switch (typeCodes[ordinal])
            {
                case TypeCode.DateTime:
                    dt = reader.GetDateTime(ordinal);
                    break;
                default:
                    dt = ReflectionTools.ChangeType<DateTime>(reader.GetValue(ordinal));
                    break;
            }

            if (Schema.Current.TimeZoneMode == TimeZoneMode.Utc)
                return new DateTime(dt.Ticks, DateTimeKind.Utc);

            return new DateTime(dt.Ticks, DateTimeKind.Local);
        }


        public DateTime? GetNullableDateTime(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableDateTime);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetDateTime(ordinal);
        }

        public Date GetDate(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetDate);
            Date dt;
            switch (typeCodes[ordinal])
            {
                case TypeCode.DateTime:
                    dt = new Date(reader.GetDateTime(ordinal));
                    break;
                case FieldReader.tcNpgsqlDate:
                    dt = new Date((DateTime)((NpgsqlDataReader)reader).GetDate(ordinal));
                    break;
                default:
                    dt = new Date(ReflectionTools.ChangeType<DateTime>(reader.GetValue(ordinal)));
                    break;
            }

            return dt;
        }

        public Date? GetNullableDate(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableDate);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetDate(ordinal);
        }

        public DateTimeOffset GetDateTimeOffset(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetDateTimeOffset);
            switch (typeCodes[ordinal])
            {
                case tcDateTimeOffset:
                    if (isPostgres)
                        throw new InvalidOperationException("DateTimeOffset not supported in Postgres");
                        
                    return ((SqlDataReader)reader).GetDateTimeOffset(ordinal);
                default:
                    return ReflectionTools.ChangeType<DateTimeOffset>(reader.GetValue(ordinal));
            }
        }

        public DateTimeOffset? GetNullableDateTimeOffset(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableDateTimeOffset);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetDateTimeOffset(ordinal);
        }


        public TimeSpan GetTimeSpan(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetTimeSpan);
            switch (typeCodes[ordinal])
            {
                case tcTimeSpan:
                    if (isPostgres)
                        return ((NpgsqlDataReader)reader).GetTimeSpan(ordinal);
                    else
                        return ((SqlDataReader)reader).GetTimeSpan(ordinal);
                default:
                    return ReflectionTools.ChangeType<TimeSpan>(reader.GetValue(ordinal));
            }
        }

        public TimeSpan? GetNullableTimeSpan(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableTimeSpan);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetTimeSpan(ordinal);
        }


        public Guid GetGuid(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetGuid);
            switch (typeCodes[ordinal])
            {
                case tcGuid:
                    return reader.GetGuid(ordinal);
                default:
                    return ReflectionTools.ChangeType<Guid>(reader.GetValue(ordinal));
            }
        }

        public Guid? GetNullableGuid(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableGuid);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return GetGuid(ordinal);
        }
      
        static MethodInfo miGetUdt = ReflectionTools.GetMethodInfo((FieldReader r) => r.GetUdt<IBinarySerialize>(0)).GetGenericMethodDefinition(); 

        public T GetUdt<T>(int ordinal)
            where T : IBinarySerialize
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetUdt) + "<" + typeof(T).Name + ">";
            if (reader.IsDBNull(ordinal))
            {
                return (T)(object)null!;
            }

            var udt = Activator.CreateInstance<T>();
            udt.Read(new BinaryReader(reader.GetStream(ordinal)));
            return udt;
        }

        static MethodInfo miGetArray = ReflectionTools.GetMethodInfo((FieldReader r) => r.GetArray<int>(0)).GetGenericMethodDefinition();

        public T[] GetArray<T>(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetUdt) + "<" + typeof(T).Name + ">";
            if (reader.IsDBNull(ordinal))
            {
                return (T[])(object)null!;
            }

            return (T[])this.reader[ordinal]; 
        }

        static MethodInfo miNullableGetRange = ReflectionTools.GetMethodInfo((FieldReader r) => r.GetNullableRange<int>(0)).GetGenericMethodDefinition();
        public NpgsqlTypes.NpgsqlRange<T>? GetNullableRange<T>(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetNullableRange) + "<" + typeof(T).Name + ">";
            if (reader.IsDBNull(ordinal))
            {
                return (NpgsqlTypes.NpgsqlRange<T>)(object)null!;
            }

            return (NpgsqlTypes.NpgsqlRange<T>)this.reader[ordinal];
        }

        static MethodInfo miGetRange = ReflectionTools.GetMethodInfo((FieldReader r) => r.GetRange<int>(0)).GetGenericMethodDefinition();
        public NpgsqlTypes.NpgsqlRange<T> GetRange<T>(int ordinal)
        {
            LastOrdinal = ordinal;
            LastMethodName = nameof(GetRange) + "<" + typeof(T).Name + ">";
            return (NpgsqlTypes.NpgsqlRange<T>)this.reader[ordinal];
        }

        static Dictionary<Type, MethodInfo> methods =
            typeof(FieldReader).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name != "GetExpression" && m.Name != "IsNull")
            .ToDictionary(a => a.ReturnType);


        public static Expression GetExpression(Expression reader, int ordinal, Type type)
        {
            MethodInfo? mi = methods.TryGetC(type);
            if (mi != null)
                return Expression.Call(reader, mi, Expression.Constant(ordinal));

            if (typeof(IBinarySerialize).IsAssignableFrom(type.UnNullify()))
            {
                if (type.IsNullable())
                    return Expression.Call(reader, miGetUdt.MakeGenericMethod(type.UnNullify()), Expression.Constant(ordinal)).Nullify();
                else
                    return Expression.Call(reader, miGetUdt.MakeGenericMethod(type.UnNullify()), Expression.Constant(ordinal));
            }

            if (type.IsArray)
            {
                return Expression.Call(reader, miGetArray.MakeGenericMethod(type.ElementType()!), Expression.Constant(ordinal));
            }

            if (type.IsInstantiationOf(typeof(NpgsqlTypes.NpgsqlRange<>)))
            {
                return Expression.Call(reader, miGetRange.MakeGenericMethod(type.GetGenericArguments()[0]!), Expression.Constant(ordinal));
            }

            if (type.IsNullable() && type.UnNullify().IsInstantiationOf(typeof(NpgsqlTypes.NpgsqlRange<>)))
            {
                return Expression.Call(reader, miGetRange.MakeGenericMethod(type.UnNullify().GetGenericArguments()[0]!), Expression.Constant(ordinal));
            }

            throw new InvalidOperationException("Type {0} not supported".FormatWith(type));
        }

        static MethodInfo miIsNull = ReflectionTools.GetMethodInfo((FieldReader r) => r.IsNull(0));

        public static Expression GetIsNull(Expression reader, int ordinal)
        {
            return Expression.Call(reader, miIsNull, Expression.Constant(ordinal));
        }

        internal FieldReaderException CreateFieldReaderException(Exception ex)
        {
            return new FieldReaderException(ex,
                ordinal: LastOrdinal,
                columnName: LastOrdinal != null ? reader.GetName(LastOrdinal.Value) : null,
                methodName: LastMethodName
            ) ;
        }
    }

    [Serializable]
    public class FieldReaderException : DbException
    {
        public int? Ordinal { get; internal set; }
        public string? ColumnName { get; internal set; }
        public string? MethodName { get; internal set; }
        public int Row { get; internal set; }
        public SqlPreCommand? Command { get; internal set; }
        public LambdaExpression? Projector { get; internal set; }

        public FieldReaderException(Exception inner, int? ordinal, string? columnName, string? methodName) : base(null, inner)
        {
            this.Ordinal = ordinal;
            this.ColumnName = columnName;
            this.MethodName = methodName;
        }
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        protected FieldReaderException(
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public override string Message
        {
            get
            {
                string text = "{0}\r\nOrdinal: {1}\r\nColumnName: {2}\r\nRow: {3}".FormatWith(InnerException!.Message, Ordinal, ColumnName, Row);

                if (Ordinal != null && MethodName != null)
                    text += "\r\nCalling: row.Reader.{0}({1})".FormatWith(MethodName, Ordinal);

                if (Projector != null)
                    text += "\r\nProjector:\r\n{0}".FormatWith(Projector.ToString().Indent(4));

                if(Command != null)
                    text += "\r\nCommand:\r\n{0}".FormatWith(Command.PlainSql().Indent(4));

                 return text;
            }
        }

       
    }
}
