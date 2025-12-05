using Signum.Utilities.Reflection;
using Signum.Engine.Maps;
using System.IO;
using Npgsql;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Server;
using Signum.Engine.Sync;

namespace Signum.Engine;

public class FieldReader
{
    readonly DbDataReader reader;
    readonly TypeCode[] typeCodes;

    private const TypeCode tcGuid = (TypeCode)20;
    private const TypeCode tcTimeSpan = (TypeCode)21;
    private const TypeCode tcDateTimeOffset = (TypeCode)22;
    private const TypeCode tcDateOnly = (TypeCode)24;
    private const TypeCode tcTimeOnly = (TypeCode)25;


    public int? LastOrdinal;
    public string? LastMethodName;

    TypeCode GetTypeCode(int ordinal)
    {
        //new PostgreSqlConnector(connectionString, sb.Schema, postgreeVersion, dsb => dsb.EnableArrays());
        Type type = reader.GetFieldType(ordinal);
        TypeCode tc = Type.GetTypeCode(type);
        if (tc == TypeCode.Object)
        {
            if (type == typeof(Guid))
                tc = tcGuid;

            if (type == typeof(TimeSpan))
                tc = tcTimeSpan;

            if (type == typeof(TimeOnly))
                tc = tcTimeOnly;

            if (type == typeof(DateTimeOffset))
                tc = tcDateTimeOffset;

            if (type == typeof(DateOnly))
                tc = tcDateOnly;
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

        return typeCodes[ordinal] switch
        {
            TypeCode.Byte => reader.GetByte(ordinal).ToString(),

            TypeCode.Int16 => reader.GetInt16(ordinal).ToString(),
            TypeCode.Int32 => reader.GetInt32(ordinal).ToString(),
            TypeCode.Int64 => reader.GetInt64(ordinal).ToString(),
            TypeCode.UInt16 => reader.GetFieldValue<UInt16>(ordinal).ToString(),
            TypeCode.UInt32 => reader.GetFieldValue<UInt32>(ordinal).ToString(),
            TypeCode.UInt64 => reader.GetFieldValue<UInt64>(ordinal).ToString(),
            TypeCode.Double => reader.GetDouble(ordinal).ToString(),
            TypeCode.Single => reader.GetFloat(ordinal).ToString(),
            TypeCode.Decimal => reader.GetDecimal(ordinal).ToString(),
            TypeCode.DateTime => reader.GetDateTime(ordinal).ToString(),
            tcGuid => reader.GetGuid(ordinal).ToString(),
            TypeCode.String => reader.GetString(ordinal),
            _ => reader.GetValue(ordinal).ToString(),
        };
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
        return typeCodes[ordinal] switch
        {
            TypeCode.Boolean => reader.GetBoolean(ordinal),
            TypeCode.Byte => reader.GetByte(ordinal) != 0,
            TypeCode.Int16 => reader.GetInt16(ordinal) != 0,
            TypeCode.Int32 => reader.GetInt32(ordinal) != 0,
            TypeCode.Int64 => reader.GetInt64(ordinal) != 0,
            TypeCode.UInt16 => reader.GetFieldValue<UInt16>(ordinal) != 0,
            TypeCode.UInt32 => reader.GetFieldValue<UInt32>(ordinal) != 0,
            TypeCode.UInt64 => reader.GetFieldValue<UInt64>(ordinal) != 0,
            TypeCode.String => bool.Parse(reader.GetString(ordinal)),
            _ => ReflectionTools.ChangeType<bool>(reader.GetValue(ordinal)),
        };
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
        return typeCodes[ordinal] switch
        {
            TypeCode.Byte => reader.GetByte(ordinal),
            TypeCode.Int16 => (Byte)reader.GetInt16(ordinal),
            TypeCode.Int32 => (Byte)reader.GetInt32(ordinal),
            TypeCode.Int64 => (Byte)reader.GetInt64(ordinal),
            TypeCode.UInt16 => (Byte)reader.GetFieldValue<UInt16>(ordinal),
            TypeCode.UInt32 => (Byte)reader.GetFieldValue<UInt32>(ordinal),
            TypeCode.UInt64 => (Byte)reader.GetFieldValue<UInt64>(ordinal),
            TypeCode.Double => (Byte)reader.GetDouble(ordinal),
            TypeCode.Single => (Byte)reader.GetFloat(ordinal),
            TypeCode.Decimal => (Byte)reader.GetDecimal(ordinal),
            _ => ReflectionTools.ChangeType<Byte>(reader.GetValue(ordinal)),
        };
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
        return typeCodes[ordinal] switch
        {
            TypeCode.Byte => (Char)reader.GetByte(ordinal),
            TypeCode.Int16 => (Char)reader.GetInt16(ordinal),
            TypeCode.Int32 => (Char)reader.GetInt32(ordinal),
            TypeCode.Int64 => (Char)reader.GetInt64(ordinal),
            TypeCode.UInt16 => (Char)reader.GetFieldValue<UInt16>(ordinal),
            TypeCode.UInt32 => (Char)reader.GetFieldValue<UInt32>(ordinal),
            TypeCode.UInt64 => (Char)reader.GetFieldValue<UInt64>(ordinal),
            TypeCode.Double => (Char)reader.GetDouble(ordinal),
            TypeCode.Single => (Char)reader.GetFloat(ordinal),
            TypeCode.Decimal => (Char)reader.GetDecimal(ordinal),
            _ => ReflectionTools.ChangeType<Char>(reader.GetValue(ordinal)),
        };
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
        return typeCodes[ordinal] switch
        {
            TypeCode.Byte => (Single)reader.GetByte(ordinal),
            TypeCode.Int16 => (Single)reader.GetInt16(ordinal),
            TypeCode.Int32 => (Single)reader.GetInt32(ordinal),
            TypeCode.Int64 => (Single)reader.GetInt64(ordinal),
            TypeCode.UInt16 => (Single)reader.GetFieldValue<UInt16>(ordinal),
            TypeCode.UInt32 => (Single)reader.GetFieldValue<UInt32>(ordinal),
            TypeCode.UInt64 => (Single)reader.GetFieldValue<UInt64>(ordinal),
            TypeCode.Double => (Single)reader.GetDouble(ordinal),
            TypeCode.Single => reader.GetFloat(ordinal),
            TypeCode.Decimal => (Single)reader.GetDecimal(ordinal),
            _ => ReflectionTools.ChangeType<Single>(reader.GetValue(ordinal)),
        };
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
        return typeCodes[ordinal] switch
        {
            TypeCode.Byte => (Double)reader.GetByte(ordinal),
            TypeCode.Int16 => (Double)reader.GetInt16(ordinal),
            TypeCode.Int32 => (Double)reader.GetInt32(ordinal),
            TypeCode.Int64 => (Double)reader.GetInt64(ordinal),
            TypeCode.UInt16 => (Double)reader.GetFieldValue<UInt16>(ordinal),
            TypeCode.UInt32 => (Double)reader.GetFieldValue<UInt32>(ordinal),
            TypeCode.UInt64 => (Double)reader.GetFieldValue<UInt64>(ordinal),
            TypeCode.Double => reader.GetDouble(ordinal),
            TypeCode.Single => (Double)reader.GetFloat(ordinal),
            TypeCode.Decimal => (Double)reader.GetDecimal(ordinal),
            _ => ReflectionTools.ChangeType<Double>(reader.GetValue(ordinal)),
        };
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
        return typeCodes[ordinal] switch
        {
            TypeCode.Byte => (Decimal)reader.GetByte(ordinal),
            TypeCode.Int16 => (Decimal)reader.GetInt16(ordinal),
            TypeCode.Int32 => (Decimal)reader.GetInt32(ordinal),
            TypeCode.Int64 => (Decimal)reader.GetInt64(ordinal),
            TypeCode.UInt16 => (Decimal)reader.GetFieldValue<UInt16>(ordinal),
            TypeCode.UInt32 => (Decimal)reader.GetFieldValue<UInt32>(ordinal),
            TypeCode.UInt64 => (Decimal)reader.GetFieldValue<UInt64>(ordinal),
            TypeCode.Double => (Decimal)reader.GetDouble(ordinal),
            TypeCode.Single => (Decimal)reader.GetFloat(ordinal),
            TypeCode.Decimal => reader.GetDecimal(ordinal),
            _ => ReflectionTools.ChangeType<Decimal>(reader.GetValue(ordinal)),
        };
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
        return typeCodes[ordinal] switch
        {
            TypeCode.Byte => (Int16)reader.GetByte(ordinal),
            TypeCode.Int16 => reader.GetInt16(ordinal),
            TypeCode.Int32 => (Int16)reader.GetInt32(ordinal),
            TypeCode.Int64 => (Int16)reader.GetInt64(ordinal),
            TypeCode.UInt16 => (Int16)reader.GetFieldValue<UInt16>(ordinal),
            TypeCode.UInt32 => (Int16)reader.GetFieldValue<UInt32>(ordinal),
            TypeCode.UInt64 => (Int16)reader.GetFieldValue<UInt64>(ordinal),
            TypeCode.Double => (Int16)reader.GetDouble(ordinal),
            TypeCode.Single => (Int16)reader.GetFloat(ordinal),
            TypeCode.Decimal => (Int16)reader.GetDecimal(ordinal),
            _ => ReflectionTools.ChangeType<Int16>(reader.GetValue(ordinal)),
        };
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
        return typeCodes[ordinal] switch
        {
            TypeCode.Byte => (Int32)reader.GetByte(ordinal),
            TypeCode.Int16 => (Int32)reader.GetInt16(ordinal),
            TypeCode.Int32 => reader.GetInt32(ordinal),
            TypeCode.Int64 => (Int32)reader.GetInt64(ordinal),
            TypeCode.UInt16 => (Int32)reader.GetFieldValue<UInt16>(ordinal),
            TypeCode.UInt32 => (Int32)reader.GetFieldValue<UInt32>(ordinal),
            TypeCode.UInt64 => (Int32)reader.GetFieldValue<UInt64>(ordinal),
            TypeCode.Double => (Int32)reader.GetDouble(ordinal),
            TypeCode.Single => (Int32)reader.GetFloat(ordinal),
            TypeCode.Decimal => (Int32)reader.GetDecimal(ordinal),
            _ => ReflectionTools.ChangeType<Int32>(reader.GetValue(ordinal)),
        };
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
        return typeCodes[ordinal] switch
        {
            TypeCode.Byte => (Int64)reader.GetByte(ordinal),
            TypeCode.Int16 => (Int64)reader.GetInt16(ordinal),
            TypeCode.Int32 => (Int64)reader.GetInt32(ordinal),
            TypeCode.Int64 => (Int64)reader.GetInt64(ordinal),
            TypeCode.UInt16 => (Int64)reader.GetFieldValue<UInt16>(ordinal),
            TypeCode.UInt32 => (Int64)reader.GetFieldValue<UInt32>(ordinal),
            TypeCode.UInt64 => (Int64)reader.GetFieldValue<UInt64>(ordinal),
            TypeCode.Double => (Int64)reader.GetDouble(ordinal),
            TypeCode.Single => (Int64)reader.GetFloat(ordinal),
            TypeCode.Decimal => (Int64)reader.GetDecimal(ordinal),
            _ => ReflectionTools.ChangeType<Int64>(reader.GetValue(ordinal)),
        };
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


    static readonly MethodInfo miGetDateTimeLocal = ReflectionTools.GetMethodInfo((FieldReader r) => r.GetDateTimeLocal(0));
    static readonly MethodInfo miGetNullableDateTimeLocal = ReflectionTools.GetMethodInfo((FieldReader r) => r.GetNullableDateTimeLocal(0));

    static readonly MethodInfo miGetDateTimeUTC = ReflectionTools.GetMethodInfo((FieldReader r) => r.GetDateTimeUtc(0));
    static readonly MethodInfo miGetNullableDateTimeUTC = ReflectionTools.GetMethodInfo((FieldReader r) => r.GetNullableDateTimeUtc(0));


    public DateTime GetDateTimeLocal(int ordinal)
    {
        LastOrdinal = ordinal;
        LastMethodName = nameof(GetDateTimeLocal);
        var dt = typeCodes[ordinal] switch
        {
            TypeCode.DateTime => reader.GetDateTime(ordinal),
            tcDateOnly => reader.GetFieldValue<DateOnly>(ordinal).ToDateTime(),
            _ => ReflectionTools.ChangeType<DateTime>(reader.GetValue(ordinal)),
        };

        return new DateTime(dt.Ticks, DateTimeKind.Local);
    }

    public DateTime GetDateTimeUtc(int ordinal)
    {
        LastOrdinal = ordinal;
        LastMethodName = nameof(GetDateTimeUtc);
        var dt = typeCodes[ordinal] switch
        {
            TypeCode.DateTime => reader.GetDateTime(ordinal),
            _ => ReflectionTools.ChangeType<DateTime>(reader.GetValue(ordinal)),
        };
        return new DateTime(dt.Ticks, DateTimeKind.Utc);
    }

        //if (Schema.Current.TimeZoneMode == TimeZoneMode.Utc)
    

    public DateTime? GetNullableDateTimeLocal(int ordinal)
    {
        LastOrdinal = ordinal;
        LastMethodName = nameof(GetNullableDateTimeLocal);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }
        return GetDateTimeLocal(ordinal);
    }

    public DateTime? GetNullableDateTimeUtc(int ordinal)
    {
        LastOrdinal = ordinal;
        LastMethodName = nameof(GetNullableDateTimeUtc);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }
        return GetDateTimeUtc(ordinal);
    }

    public DateOnly GetDateOnly(int ordinal)
    {
        LastOrdinal = ordinal;
        LastMethodName = nameof(GetDateOnly);
        var dt = typeCodes[ordinal] switch
        {
            TypeCode.DateTime => reader.GetDateTime(ordinal).ToDateOnly(),
            tcDateOnly => reader.GetFieldValue<DateOnly>(ordinal),
            _ => reader.GetFieldValue<DateOnly>(ordinal),
        };
        return dt;
    }

    public DateOnly? GetNullableDateOnly(int ordinal)
    {
        LastOrdinal = ordinal;
        LastMethodName = nameof(GetNullableDateOnly);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }
        return GetDateOnly(ordinal);
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
            case tcTimeOnly:
                return reader.GetFieldValue<TimeOnly>(ordinal).ToTimeSpan();
            default:
                return reader.GetFieldValue<TimeSpan>(ordinal);
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

    public TimeOnly GetTimeOnly(int ordinal)
    {
        LastOrdinal = ordinal;
        LastMethodName = nameof(GetTimeOnly);
        switch (typeCodes[ordinal])
        {
            case tcTimeOnly:
                return reader.GetFieldValue<TimeOnly>(ordinal);
            case tcTimeSpan:
                if (isPostgres)
                    return TimeOnly.FromTimeSpan(((NpgsqlDataReader)reader).GetTimeSpan(ordinal));
                else
                    return TimeOnly.FromTimeSpan(((SqlDataReader)reader).GetTimeSpan(ordinal));
            default:
                return TimeOnly.FromTimeSpan(reader.GetFieldValue<TimeSpan>(ordinal));
        }
    }

    public TimeOnly? GetNullableTimeOnly(int ordinal)
    {
        LastOrdinal = ordinal;
        LastMethodName = nameof(GetNullableTimeOnly);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }
        return GetTimeOnly(ordinal);
    }


    public Guid GetGuid(int ordinal)
    {
        LastOrdinal = ordinal;
        LastMethodName = nameof(GetGuid);
        return typeCodes[ordinal] switch
        {
            tcGuid => reader.GetGuid(ordinal),
            _ => reader.GetFieldValue<Guid>(ordinal),
        };
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
  

    static readonly MethodInfo miGetUdt = ReflectionTools.GetMethodInfo((FieldReader r) => r.GetUdt<UdtExample>(0)).GetGenericMethodDefinition(); 
    public T GetUdt<T>(int ordinal) where T : IBinarySerialize
    {
        LastOrdinal = ordinal;
        LastMethodName = nameof(GetUdt) + "<" + typeof(T).Name + ">";
        var udt = Activator.CreateInstance<T>();
        udt.Read(new BinaryReader(reader.GetStream(ordinal)));
        return udt;
    }

    struct UdtExample : IBinarySerialize
    {
        public void Read(BinaryReader r) => throw new NotImplementedException();
        public void Write(BinaryWriter w) => throw new NotImplementedException();
    }

    static readonly MethodInfo miGetNullableUdt = ReflectionTools.GetMethodInfo((FieldReader r) => r.GetNullableUdt<UdtExample>(0)).GetGenericMethodDefinition();
    public T? GetNullableUdt<T>(int ordinal) where T : struct, IBinarySerialize
    {
        LastOrdinal = ordinal;
        LastMethodName = nameof(GetNullableUdt) + "<" + typeof(T).Name + ">";
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var udt = Activator.CreateInstance<T>();
        udt.Read(new BinaryReader(reader.GetStream(ordinal)));
        return udt;
    }


    static readonly MethodInfo miGetArray = ReflectionTools.GetMethodInfo((FieldReader r) => r.GetArray<int>(0)).GetGenericMethodDefinition();

    public T[] GetArray<T>(int ordinal)
    {
        LastOrdinal = ordinal;
        LastMethodName = nameof(GetArray) + "<" + typeof(T).Name + ">";
        if (reader.IsDBNull(ordinal))
        {
            return (T[])(object)null!;
        }

        return (T[])this.reader[ordinal]; 
    }

    static readonly MethodInfo miNullableGetRange = ReflectionTools.GetMethodInfo((FieldReader r) => r.GetNullableRange<int>(0)).GetGenericMethodDefinition();
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

    static readonly MethodInfo miGetRange = ReflectionTools.GetMethodInfo((FieldReader r) => r.GetRange<int>(0)).GetGenericMethodDefinition();
    public NpgsqlTypes.NpgsqlRange<T> GetRange<T>(int ordinal)
    {
        LastOrdinal = ordinal;
        LastMethodName = nameof(GetRange) + "<" + typeof(T).Name + ">";
        return (NpgsqlTypes.NpgsqlRange<T>)this.reader[ordinal];
    }

    static Dictionary<Type, MethodInfo> methods =
        typeof(FieldReader).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Where(m => m.Name != "GetExpression" && m.Name != "IsNull" && m.ReturnType.UnNullify() != typeof(DateTime))
        .ToDictionary(a => a.ReturnType);


    public static Expression GetExpression(Expression reader, int ordinal, Type type, DateTimeKind kind)
    {
        MethodInfo? mi = methods.TryGetC(type);
        if (mi != null)
            return Expression.Call(reader, mi, Expression.Constant(ordinal));

        if (type.UnNullify() == typeof(DateTime))
        {
            mi = kind switch
            {
                DateTimeKind.Utc => type.IsNullable() ? miGetNullableDateTimeUTC : miGetDateTimeUTC,
                DateTimeKind.Local => type.IsNullable() ? miGetNullableDateTimeLocal : miGetDateTimeLocal,
                DateTimeKind.Unspecified or _ => throw new UnexpectedValueException(DateTimeKind.Unspecified)
            };

            return Expression.Call(reader, mi, Expression.Constant(ordinal));
        }

        if (typeof(IBinarySerialize).IsAssignableFrom(type.UnNullify()))
        {
            if (type.IsNullable())
                return Expression.Call(reader, miGetNullableUdt.MakeGenericMethod(type.UnNullify()), Expression.Constant(ordinal));
            else
                return Expression.Call(reader, miGetUdt.MakeGenericMethod(type), Expression.Constant(ordinal));
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
            return Expression.Call(reader, miNullableGetRange.MakeGenericMethod(type.UnNullify().GetGenericArguments()[0]!), Expression.Constant(ordinal));
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

    public override string Message
    {
        get
        {
            string text = "{0}\nOrdinal: {1}\nColumnName: {2}\nRow: {3}".FormatWith(InnerException!.Message, Ordinal, ColumnName, Row);

            if (Ordinal != null && MethodName != null)
                text += "\nCalling: row.Reader.{0}({1})".FormatWith(MethodName, Ordinal);

            if (Projector != null)
                text += "\nProjector:\n{0}".FormatWith(Projector.ToStringIndented().Indent(4));

            if(Command != null)
                text += "\nCommand:\n{0}".FormatWith(Command.PlainSql().Indent(4));

             return text;
        }
    }

   
}
