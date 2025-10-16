using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Signum.Basics;

public static class PropertyRouteTranslationLogic
{
    public static Dictionary<Type, Dictionary<PropertyRoute, TranslatableRouteType>> TranslateableRoutes = new Dictionary<Type, Dictionary<PropertyRoute, TranslatableRouteType>>();

    public static bool IsActivated { get; set; }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Schema.SchemaCompleted += () =>
        {
            var s = Schema.Current;

            var prs = (from t in s.Tables.Keys
                       from pr in PropertyRoute.GenerateRoutes(t)
                       where pr.PropertyRouteType == PropertyRouteType.FieldOrProperty && pr.FieldInfo != null && pr.FieldInfo.FieldType == typeof(string) &&
                       s.Settings.FieldAttribute<TranslatableAttribute>(pr) != null &&
                       s.Settings.FieldAttribute<IgnoreAttribute>(pr) == null
                       select KeyValuePair.Create(pr, s.Settings.FieldAttribute<TranslatableAttribute>(pr)!.TranslatableRouteType)).ToList();

            foreach (var kvp in prs)
            {
                RegisterRoute(kvp.Key, kvp.Value);
            }
        };
    }


    public static void RegisterRoute<T, S>(Expression<Func<T, S>> propertyRoute, TranslatableRouteType type = TranslatableRouteType.Text) where T : Entity
    {
        RegisterRoute(PropertyRoute.Construct(propertyRoute), type);
    }

    public static void RegisterRoute(PropertyRoute route, TranslatableRouteType type = TranslatableRouteType.Text)
    {
        if (route.PropertyRouteType != PropertyRouteType.FieldOrProperty)
            throw new InvalidOperationException("Routes of type {0} can not be traducibles".FormatWith(route.PropertyRouteType));

        if (route.Type != typeof(string))
            throw new InvalidOperationException("Only string routes can be traducibles");

        TranslateableRoutes.GetOrCreate(route.RootType).Add(route, type);
    }

    public static bool IsTranslateable(PropertyRoute route)
    {
        return IsActivated && TranslateableRoutes.TryGetC(route.RootType)?.ContainsKey(route) == true;
    }

    public static TranslatableRouteType? RouteType(PropertyRoute route)
    {
        var dic = TranslateableRoutes.TryGetC(route.RootType);

        return dic?.TryGetS(route);
    }

    static ConcurrentDictionary<LambdaExpression, Delegate> compiledExpressions = new ConcurrentDictionary<LambdaExpression, Delegate>(ExpressionComparer.GetComparer<LambdaExpression>(false));

    public static Func<T, R> GetPropertyRouteAccesor<T, R>(Expression<Func<T, R>> propertyRoute)
    {
        return (Func<T, R>)compiledExpressions.GetOrAdd(propertyRoute, ld => ld.Compile());
    }

    public static string TranslatedField<T>(this T entity, Expression<Func<T, string>> property) where T : Entity
    {
        string? fallbackString = GetPropertyRouteAccesor(property)(entity);

        var pr = PropertyRoute.Construct(property);

        return entity.ToLite().TranslatedField(pr, fallbackString);
    }

    public static string? TranslatedFieldNullable<T>(this T entity, Expression<Func<T, string?>> property) where T : Entity
    {
        string? fallbackString = GetPropertyRouteAccesor(property)(entity);

        var pr = PropertyRoute.Construct(property);

        return entity.ToLite().TranslatedField(pr, fallbackString);
    }

    public static IEnumerable<TranslatableElement<T>> TranslatedMList<E, T>(this E entity, Expression<Func<E, MList<T>>> mlistProperty) where E : Entity
    {
        var mlist = GetPropertyRouteAccesor(mlistProperty);

        PropertyRoute route = PropertyRoute.Construct(mlistProperty).Add("Item");

        var lite = entity.ToLite();

        foreach (var item in ((IMListPrivate<T>)mlist(entity)).InnerList)
        {
            yield return new TranslatableElement<T>(lite, route, item);
        }
    }

    public static string TranslatedElement<T>(this TranslatableElement<T> element, Expression<Func<T, string>> property)
    {
        string fallback = GetPropertyRouteAccesor(property)(element.Value);

        PropertyRoute route = element.ElementRoute.Continue(property);

        return TranslatedField(element.Lite, route, element.RowId, fallback);
    }

    public static string? TranslatedElementNullable<T>(this TranslatableElement<T> element, Expression<Func<T, string?>> property)
    {
        string? fallback = GetPropertyRouteAccesor(property)(element.Value);

        PropertyRoute route = element.ElementRoute.Continue(property);

        return TranslatedField(element.Lite, route, element.RowId, fallback);
    }


    [return: NotNullIfNotNull("fallbackString")]
    public static string? TranslatedField<T>(this Lite<T> lite, Expression<Func<T, string?>> property, string? fallbackString) where T : Entity
    {
        PropertyRoute route = PropertyRoute.Construct(property);

        return lite.TranslatedField(route, fallbackString);
    }


    static Expression<Func<Lite<Entity>, PropertyRoute, string?, string?>> TranslatedFieldSimpleExpression = 
        (Lite<Entity> lite, PropertyRoute route, string? fallbackString) => TranslatedFieldExpression!.Evaluate(lite, route, null, fallbackString);
    [ExpressionField("TranslatedFieldSimpleExpression")]
    [return: NotNullIfNotNull("fallbackString")]
    public static string? TranslatedField(this Lite<Entity> lite, PropertyRoute route, string? fallbackString)
    {
        return TranslatedField(lite, route, null, fallbackString);
    }


    public static Func<Lite<Entity>, PropertyRoute, PrimaryKey?, string?, string?> TranslatedFieldFunc = 
        (Lite<Entity> lite, PropertyRoute route, PrimaryKey? rowId, string? fallbackString) => fallbackString;
    public static Expression<Func<Lite<Entity>, PropertyRoute, PrimaryKey?, string?, string?>> TranslatedFieldExpression = 
        (Lite<Entity> lite, PropertyRoute route, PrimaryKey? rowId, string? fallbackString) => fallbackString;

    public static MethodInfo miTranslatedField = ReflectionTools.GetMethodInfo(() => TranslatedField((Lite<Entity>)null!, (PropertyRoute)null!, (PrimaryKey?)null, (string?)null));

    [return: NotNullIfNotNull("fallbackString")]
    [ExpressionField("TranslatedFieldExpression")]
    public static string? TranslatedField(this Lite<Entity> lite, PropertyRoute route, PrimaryKey? rowId, string? fallbackString)
    {
        return TranslatedFieldFunc(lite, route, rowId, fallbackString);
    }


}

public struct TranslatableElement<T>
{
    public readonly Lite<Entity> Lite;
    public readonly PropertyRoute ElementRoute;
    public readonly T Value;
    public readonly PrimaryKey RowId;

    internal TranslatableElement(Lite<Entity> entity, PropertyRoute route, MList<T>.RowIdElement item)
    {
        Lite = entity;
        ElementRoute = route;
        Value = item.Element;
        RowId = item.RowId!.Value;
    }
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class TranslatableAttribute(TranslatableRouteType translatableRouteType = TranslatableRouteType.Text) : Attribute
{
    public TranslatableRouteType TranslatableRouteType = translatableRouteType;
}

[InTypeScript(true)]
public enum TranslatableRouteType
{
    Text,
    Html
}
