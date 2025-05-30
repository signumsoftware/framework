using Signum.Utilities.Reflection;

namespace Signum.Entities;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BindParentAttribute : Attribute
{

}


//Used by BindParentAttribute
internal static class AttributeManager<T>
    where T : Attribute
{
    //Consider using ImmutableAVLTree instead
    readonly static Dictionary<Type, TypeAttributePack?> fieldAndProperties = new Dictionary<Type, TypeAttributePack?>();

    static TypeAttributePack? GetFieldsAndProperties(Type type)
    {
        lock (fieldAndProperties)
        {
            return fieldAndProperties.GetOrCreate(type, () =>
            {
                var list = Reflector.InstanceFieldsInOrder(type).Where(fi => fi.HasAttribute<T>() || (Reflector.TryFindPropertyInfo(fi)?.HasAttribute<T>() ?? false)).ToList();

                if (list.Count == 0)
                    return null;

                return new TypeAttributePack(
                    fields: list.Select(fi => ReflectionTools.CreateGetter<ModifiableEntity, object?>(fi)!).ToArray(),
                    propertyNames: list.Select(fi => Reflector.FindPropertyInfo(fi).Name).ToArray()
                );
            });
        }
    }

    public static bool FieldContainsAttribute(Type type, PropertyInfo pi)
    {
        TypeAttributePack? pack = GetFieldsAndProperties(type);

        if (pack == null)
            return false;

        return pack.PropertyNames.Contains(pi.Name);
    }

    readonly static object[] EmptyArray = new object[0];

    public static object?[] FieldsWithAttribute(ModifiableEntity entity)
    {
        TypeAttributePack? pack = GetFieldsAndProperties(entity.GetType());

        if (pack == null)
            return EmptyArray;

        return pack.Fields.Select(f => f(entity)).ToArray();
    }

    public static string? FindPropertyName(ModifiableEntity entity, object fieldValue)
    {
        TypeAttributePack? pack = GetFieldsAndProperties(entity.GetType());

        if (pack == null)
            return null;

        int index = pack.Fields.IndexOf(f => f(entity) == fieldValue);

        if (index == -1)
            return null;

        return pack.PropertyNames[index];
    }
}

internal class TypeAttributePack
{
    public Func<ModifiableEntity, object?>[] Fields;
    public string[] PropertyNames;

    public TypeAttributePack(Func<ModifiableEntity, object?>[] fields, string[] propertyNames)
    {
        Fields = fields;
        PropertyNames = propertyNames;
    }
}
