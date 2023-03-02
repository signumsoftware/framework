using System.ComponentModel;
using System.Globalization;
using Signum.Utilities.Reflection;
using System.Runtime.Serialization;

namespace Signum.Entities;

/// <summary>
/// Represents a PrimaryKey of type int, long, Guid or string, for example.
/// Its a struct to avoid another object in heap
/// The default value represents an invalid state.
/// </summary>
[TypeConverter(typeof(PrimaryKeyTypeConverter))]
public struct PrimaryKey : IEquatable<PrimaryKey>, IComparable, IComparable<PrimaryKey>, ISerializable, IFormattable
{
    public static Dictionary<Type, Type> PrimaryKeyType = new Dictionary<Type, Type>();
    public static Dictionary<PropertyRoute, Type> MListPrimaryKeyType = new Dictionary<PropertyRoute, Type>();

    public static Type Type(Type entityType)
    {
        return PrimaryKeyType.GetOrThrow(entityType);
    }

    public static Type MListType(PropertyRoute mlistPropertyRoute)
    {
        return MListPrimaryKeyType.GetOrThrow(mlistPropertyRoute);
    }

    public static void SetType(Type entityType, Type primaryKeyType)
    {
        PrimaryKeyType.Add(entityType, primaryKeyType);
    }

    public static void SetMListType(PropertyRoute mlistPropertyRoute, Type primaryKeyType)
    {
        MListPrimaryKeyType.Add(mlistPropertyRoute, primaryKeyType);
    }

    public readonly string? VariableName; //Used for Sync scenarios
    public readonly IComparable Object;

    public PrimaryKey(IComparable obj)
    {
        this.Object = obj ?? throw new ArgumentNullException(nameof(obj));
        this.VariableName = null;
    }

    public PrimaryKey(IComparable obj, string variableName)
    {
        this.Object = obj ?? throw new ArgumentNullException(nameof(obj));
        this.VariableName = variableName;
    }

    public override string ToString()
    {
        return Object.ToString()!;
    }

    public override bool Equals(object? obj)
    {
        return obj is PrimaryKey pk && this.Equals(pk);
    }

    public override int GetHashCode()
    {
        return Object.GetHashCode();
    }

    public bool Equals(PrimaryKey other)
    {
        if (other.Object.GetType() != this.Object.GetType())
            throw new InvalidOperationException("Comparing PrimaryKey of types {0} with anotherone of the {1}".FormatWith(other.Object.GetType(), this.Object.GetType()));

        return other.Object.Equals(this.Object);
    }

    public int CompareTo(object? obj)
    {
        return CompareTo((PrimaryKey)obj!);
    }

    public int CompareTo(PrimaryKey other)
    {
        if (other.Object.GetType() != this.Object.GetType())
            throw new InvalidOperationException("Comparing PrimaryKey of types {0} with anotherone of the {1}".FormatWith(other.Object.GetType(), this.Object.GetType()));

        return this.Object.CompareTo(other.Object);
    }

    public static implicit operator PrimaryKey(int id)
    {
        return new PrimaryKey(id);
    }

    public static implicit operator PrimaryKey?(int? id)
    {
        if (id == null)
            return null;

        return new PrimaryKey(id.Value);
    }

    public static implicit operator PrimaryKey(long id)
    {
        return new PrimaryKey(id);
    }

    public static implicit operator PrimaryKey?(long? id)
    {
        if (id == null)
            return null;

        return new PrimaryKey(id.Value);
    }

    public static implicit operator PrimaryKey(Guid id)
    {
        return new PrimaryKey(id);
    }

    public static implicit operator PrimaryKey?(Guid? id)
    {
        if (id == null)
            return null;

        return new PrimaryKey(id.Value);
    }

    public static implicit operator PrimaryKey(DateTime id)
    {
        return new PrimaryKey(id);
    }

    public static implicit operator PrimaryKey? (DateTime? id)
    {
        if (id == null)
            return null;

        return new PrimaryKey(id.Value);
    }


    public static explicit operator int(PrimaryKey key)
    {
        return (int)key.Object;
    }

    public static explicit operator int?(PrimaryKey? key)
    {
        if (key == null)
            return null;

        return (int)key.Value.Object;
    }

    public static explicit operator long(PrimaryKey key)
    {
        return (long)key.Object;
    }

    public static explicit operator long?(PrimaryKey? key)
    {
        if (key == null)
            return null;

        return (long)key.Value.Object;
    }

    public static explicit operator Guid(PrimaryKey key)
    {
        return (Guid)key.Object;
    }

    public static explicit operator Guid?(PrimaryKey? key)
    {
        if (key == null)
            return null;

        return (Guid)key.Value.Object;
    }

    public static explicit operator DateTime(PrimaryKey key)
    {
        return (DateTime)key.Object;
    }

    public static explicit operator DateTime? (PrimaryKey? key)
    {
        if (key == null)
            return null;

        return (DateTime)key.Value.Object;
    }

    public static bool operator ==(PrimaryKey a, PrimaryKey b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(PrimaryKey a, PrimaryKey b)
    {
        return !a.Equals(b);
    }

    public static bool operator <=(PrimaryKey a, PrimaryKey b)
    {
        return a.Object.CompareTo(b.Object) <= 0;
    }

    public static bool operator <(PrimaryKey a, PrimaryKey b)
    {
        return a.Object.CompareTo(b.Object) < 0;
    }

    public static bool operator >=(PrimaryKey a, PrimaryKey b)
    {
        return a.Object.CompareTo(b.Object) >= 0;
    }

    public static bool operator >(PrimaryKey a, PrimaryKey b)
    {
        return a.Object.CompareTo(b.Object) > 0;
    }

    public static bool TryParse(string value, Type entityType, out PrimaryKey id)
    {
        if (ReflectionTools.TryParse(value, Type(entityType), out object? val))
        {
            id = new PrimaryKey((IComparable)val!);
            return true;
        }
        else
        {
            id = default(PrimaryKey);
            return false;
        }
    }

    public static PrimaryKey Parse(string value, Type entityType)
    {
        return new PrimaryKey((IComparable)ReflectionTools.Parse(value, Type(entityType))!);
    }

    public static PrimaryKey? Wrap(IComparable value)
    {
        if (value == null)
            return null;

        return new PrimaryKey(value);
    }

    public static IComparable? Unwrap(PrimaryKey? id)
    {
        if (id == null)
            return null;

        return id.Value.Object;
    }

    public string ToString(string format)
    {
        return ((IFormattable)this.Object).ToString(format, CultureInfo.CurrentCulture);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ((IFormattable)this.Object).ToString(format, formatProvider);
    }

    private PrimaryKey(SerializationInfo info, StreamingContext ctxt)
    {
        this.Object = null!;
        this.VariableName = null!;
        foreach (SerializationEntry item in info)
        {
            switch (item.Name)
            {
                case "Object": this.Object = (IComparable)item.Value!; break;
            }
        }

        if (this.Object == null)
            throw new SerializationException("Object not set");
    }


    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("Object", this.Object);
    }


}

class PrimaryKeyTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return
            sourceType == typeof(int) || sourceType == typeof(int?) ||
            sourceType == typeof(long) || sourceType == typeof(long?) ||
            sourceType == typeof(Guid) || sourceType == typeof(Guid?);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return
           destinationType == typeof(int) || destinationType == typeof(int?) ||
           destinationType == typeof(long) || destinationType == typeof(long?) ||
           destinationType == typeof(Guid) || destinationType == typeof(Guid?);
    }

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return new PrimaryKey((IComparable)value);
    }

    public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        return ((PrimaryKey)value!).Object;
    }
}
