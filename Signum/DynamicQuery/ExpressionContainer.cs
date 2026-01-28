using Signum.Engine.Linq;
using Signum.DynamicQuery.Tokens;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using Signum.Engine.Maps;
using System.Runtime.CompilerServices;

namespace Signum.DynamicQuery;

public class ExpressionContainer
{
    public Polymorphic<Dictionary<string, ExtensionInfo>> RegisteredExtensions =
        new Polymorphic<Dictionary<string, ExtensionInfo>>(PolymorphicMerger.InheritDictionaryInterfaces, null);

    public Dictionary<Type, Dictionary<string, IExtensionDictionaryInfo>> RegisteredExtensionsWithParameter =
        new Dictionary<Type, Dictionary<string, IExtensionDictionaryInfo>>();

    internal Expression BuildExtension(Type parentType, string key, Expression parentExpression)
    {
        var extensionInfo = CompatibleTypes(parentType)
            .Select(t => RegisteredExtensions.TryGetValue(t)?.TryGetC(key))
            .NotNull()
            .FirstEx(() => $"No Extension found for '{parentType.TypeName()}' and key '{key}'");

        var lambda = extensionInfo.Lambda;

        var targetType = lambda.Parameters[0].Type;

        var pe = targetType.IsAssignableFrom(parentExpression.Type) ? parentExpression :
                 targetType.IsAssignableFrom(parentExpression.Type.CleanType()) ? parentExpression.ExtractEntity(false) :
                targetType.IsAssignableFrom(parentExpression.Type.BuildLite()) ? parentExpression.BuildLite() :
                targetType == parentExpression.Type.Nullify() ? parentExpression.Nullify() :
                targetType == parentExpression.Type.UnNullify() ? parentExpression.UnNullify() :
                parentExpression;

        return ExpressionReplacer.Replace(Expression.Invoke(extensionInfo.Lambda, pe));
    }

    public IEnumerable<Type> CompatibleTypes(Type type)
    {
        yield return type;
        if (type.IsValueType)
        {
            if (type.IsNullable())
                yield return type.UnNullify();
            else
                yield return type.Nullify();
        }
        else
        {

            if (type.IsLite())
                yield return type.CleanType();
            else if (type.IsEntity())
                yield return type.BuildLite();
        }
    }

    public IEnumerable<QueryToken> GetExtensionsTokens(QueryToken parent)
    {
        var parentTypeClean = parent.Type.CleanType();

        var compatibleTypes = CompatibleTypes(parent.Type);

        var dic = compatibleTypes
            .Select(t => RegisteredExtensions.TryGetValue(t))
            .Aggregate((Dictionary<string, ExtensionInfo>?)null, (dic1, dic2) =>
           {
               if (dic1 == null)
                   return dic2;

               if (dic2 == null)
                   return dic1;

               var dic = new Dictionary<string, ExtensionInfo>();
               dic.SetRange(dic1);
               dic.DefaultRange(dic2);
               return dic;
           });

        IEnumerable<QueryToken> extensionsTokens = dic == null ? Enumerable.Empty<QueryToken>() :
            dic.Values.Where(ei => ei.Inherit || compatibleTypes.Contains(ei.SourceType))
            .Where(ei => ei.IsApplicable == null || ei.IsApplicable(parent))
            .Select(v => v.CreateToken(parent));


        return extensionsTokens;
    }

    public IEnumerable<QueryToken> GetExtensionsWithParameterTokens(QueryToken parent)
    {
        var parentTypeClean = parent.Type.CleanType();

        var edi = RegisteredExtensionsWithParameter.TryGetC(parentTypeClean);

        IEnumerable<QueryToken> dicExtensionsTokens = edi == null ? Enumerable.Empty<QueryToken>() :
            edi.Values.Select(e => new IndexerContainerToken(parent, e));

        return dicExtensionsTokens;
    }

    public ExtensionInfo Register<E, S>(Expression<Func<E, S>> lambdaToMethodOrProperty, Enum niceName) =>
        Register(lambdaToMethodOrProperty, () => niceName.NiceToString());

    public ExtensionInfo Register<E, S>(Expression<Func<E, S>> lambdaToMethodOrProperty, Func<string>? niceName = null)
    {
        using (HeavyProfiler.LogNoStackTrace("RegisterExpression"))
        {
            if (lambdaToMethodOrProperty.Body.NodeType == ExpressionType.Call)
            {
                var mi = ReflectionTools.GetMethodInfo(lambdaToMethodOrProperty);

                if(mi.GetCustomAttribute<ExpressionFieldAttribute>() == null)
                    throw new InvalidOperationException("The parameter 'lambdaToMethodOrProperty' should be an expression calling a expression method or property");

                return Register<E, S>(lambdaToMethodOrProperty, niceName ?? (() => mi.Name.SpacePascalOrUnderscores()), mi.Name);
            }
            else if (lambdaToMethodOrProperty.Body.NodeType == ExpressionType.MemberAccess)
            {
                var pi = ReflectionTools.GetPropertyInfo(lambdaToMethodOrProperty);

                return Register<E, S>(lambdaToMethodOrProperty, niceName ?? (() => pi.NiceName()), pi.Name);
            }
            else 
                throw new InvalidOperationException("argument 'lambdaToMethodOrProperty' should be a simple lambda calling a method or property: {0}".FormatWith(lambdaToMethodOrProperty.ToString()));
        }
    }

    public ExtensionInfo Register<E, S>(Expression<Func<E, S>> extensionLambda, Func<string> niceName, string key, bool replace = false)
    {
        var extension = new ExtensionInfo(typeof(E), extensionLambda, typeof(S), key, niceName);

        return Register(extension);
    }

    public ExtensionInfo Register(ExtensionInfo extension, bool replace = false)
    {
        var dic = RegisteredExtensions.GetOrAddDefinition(extension.SourceType);

        if (replace)
            dic[extension.Key] = extension;
        else
            dic.Add(extension.Key, extension);

        RegisteredExtensions.ClearCache();

        return extension;
    }

    public ExtensionWithParameterInfo<T, K, V> RegisterWithParameter<T, K, V>(
        string prefix,
        Func<string> niceName,
        Func<QueryToken, IEnumerable<K>> getKeys,
        Expression<Func<T, K, V>> extensionLambda,
        bool autoExpand = false)
        where K : notnull
    {
        var ei = new ExtensionWithParameterInfo<T, K, V>(extensionLambda, prefix, niceName, getKeys);

        RegisteredExtensionsWithParameter.GetOrCreate(typeof(T)).Add(ei.Prefix, ei);

        ei.AutoExpand = autoExpand;

        return ei;
    }

    public ExtensionWithParameterInfo<T, K, V> RegisterWithParameter<T, K, V>(
        Enum message,
        Func<QueryToken, IEnumerable<K>> getKeys,
        Expression<Func<T, K, V>> extensionLambda,
        bool autoExpand = false)
        where K : notnull
    {
        var ei = new ExtensionWithParameterInfo<T, K, V>(extensionLambda, message.ToString(), ()=> message.NiceToString(), getKeys);

        RegisteredExtensionsWithParameter.GetOrCreate(typeof(T)).Add(ei.Prefix, ei);

        ei.AutoExpand = autoExpand;

        return ei;
    }
} 

public interface IExtensionDictionaryInfo
{
    IEnumerable<QueryToken> GetAllTokens(IndexerContainerToken parent);
    string? GetAllowed(IndexerContainerToken container);

    bool AutoExpand { get; } 
    Func<string> NiceName { get; } 
    string Prefix { get; } 
}

public class ExtensionWithParameterInfo<T, K, V> : IExtensionDictionaryInfo
{
    public string Prefix { get; set; }
    public Func<string> NiceName { get; set; }
    public Func<QueryToken, IEnumerable<K>> AllKeys { get; set; }
    public Func<QueryToken, string?> IsAllowed { get; set; }

    public Expression<Func<T, K, V>> LambdaExpression { get; set; }
    public bool AutoExpand { get; set; }

    readonly ConcurrentDictionary<QueryToken, ExtensionRouteInfo> metas = new ConcurrentDictionary<QueryToken, ExtensionRouteInfo>();

    public ExtensionWithParameterInfo(
        Expression<Func<T, K, V>> expression,
        string prefix,
        Func<string> niceName,
        Func<QueryToken, IEnumerable<K>> allKeys)
    {
        LambdaExpression = expression;
        Prefix = prefix;
        this.NiceName = niceName;
        AllKeys = allKeys;

    }

    public string? GetAllowed(IndexerContainerToken container)
    {
        var allowed = container.Parent!.IsAllowed();

        if (allowed != null)
            return allowed;

        var info = GetExtensionRouteInfo(container);

        return info.IsAllowed?.Invoke();
    }

    public IEnumerable<QueryToken> GetAllTokens(IndexerContainerToken container)
    {
        ExtensionRouteInfo info = GetExtensionRouteInfo(container);

        return AllKeys(container.Parent!).Select(key => new ExtensionWithParameterToken<T, K, V>(container,
            parameterValue: key,
            unit: info.Unit,
            format: info.Format,
            implementations: info.Implementations,
            propertyRoute: info.PropertyRoute,
            lambda: t => LambdaExpression.Evaluate(t, key)
        ));
    }

    private ExtensionRouteInfo GetExtensionRouteInfo(IndexerContainerToken container)
    {
        return metas.GetOrAdd(container, qt =>
        {
            var defaultValue = typeof(K).IsValueType && !typeof(K).IsNullable() ? Activator.CreateInstance<K>() : (K)(object)null!;

            Expression<Func<T, V>> lambda = t => LambdaExpression.Evaluate(t, defaultValue);

            Expression e = MetadataVisitor.JustVisit(lambda, MetaExpression.FromToken(qt, typeof(T)));

            var result = new ExtensionRouteInfo();

            if (e is MetaExpression me && me.Meta is CleanMeta cm && cm.PropertyRoutes.Any())
            {
                var cleanType = me!.Type.CleanType();

                result.PropertyRoute = cm.PropertyRoutes.Only();
                result.Implementations = me.Meta.Implementations;
                result.Format = ColumnDescriptionFactory.CombineFormat(cm.PropertyRoutes);
                result.Unit = ColumnDescriptionFactory.CombineUnit(cm.PropertyRoutes);
                result.IsAllowed = () => me.Meta.IsAllowed();
            }

            return result;
        });
    }


}

//To allow override null
public class Box<T>
{
    public T Value { get; }
    public Box(T value)
    {
        Value = value;
    }

    public static implicit operator Box<T>(T value) => new Box<T>(value);
}

public class ExtensionInfo
{
    ConcurrentDictionary<QueryToken, ExtensionRouteInfo> metas = new ConcurrentDictionary<QueryToken, ExtensionRouteInfo>();

    public ExtensionInfo(Type sourceType, LambdaExpression lambda, Type type, string key, Func<string> niceName)
    {
        this.Type = type;
        this.SourceType = sourceType;
        this.Key = key;
        this.Lambda = lambda;
        this.IsProjection = type != typeof(string) && type.ElementType() != null;
        this.NiceName = niceName;
    }

    public readonly Type Type;
    public readonly Type SourceType;
    public readonly string Key;
    public Func<QueryToken, bool>? IsApplicable;
    public readonly bool IsProjection;
    public bool Inherit = true;

    public Box<Implementations?>? ForceImplementations;
    public Box<PropertyRoute?>? ForcePropertyRoute;
    public Box<string?>? ForceFormat;
    public Box<string?>? ForceUnit;
    public Func<string?>? ForceIsAllowed;
    public bool AutoExpand = false;

    internal readonly LambdaExpression Lambda;
    public Func<string> NiceName;

    protected internal virtual ExtensionToken CreateToken(QueryToken parent)
    {
        var info = metas.GetOrAdd(parent, qt =>
        {
            Expression e = MetadataVisitor.JustVisit(Lambda, MetaExpression.FromToken(qt, SourceType));

            MetaExpression? me;

            if (this.IsProjection)
            {
                var mpe = e as MetaProjectorExpression;

                if (mpe == null)
                    mpe = MetadataVisitor.AsProjection(e);

                me = mpe == null ? null : mpe.Projector as MetaExpression;
            }
            else
            {
                me = e as MetaExpression;
            }

            var result = new ExtensionRouteInfo();

            if (me != null && me.Meta is CleanMeta cleanMeta && cleanMeta.PropertyRoutes.Any())
            {
                result.PropertyRoute = cleanMeta.PropertyRoutes.Only();
                result.Implementations = me.Meta.Implementations;
                result.Format = ColumnDescriptionFactory.CombineFormat(cleanMeta.PropertyRoutes);
                result.Unit = ColumnDescriptionFactory.CombineUnit(cleanMeta.PropertyRoutes);
            }
            else if (me?.Meta is DirtyMeta dirtyMeta)
            {
                result.PropertyRoute = dirtyMeta.CleanMetas.Select(cm => cm.PropertyRoutes.Only()).Distinct().Only();
                var metaImps = dirtyMeta.CleanMetas.Select(cm => cm.Implementations).Distinct().Only();
                if (metaImps.HasValue && !metaImps.Value.IsByAll && metaImps.Value.Types.All(t => t.IsAssignableFrom(Type)))
                {
                    result.Implementations = metaImps;
                }
                result.Format = ColumnDescriptionFactory.CombineFormat(dirtyMeta.CleanMetas.SelectMany(a => a.PropertyRoutes).ToArray());
                result.Unit = ColumnDescriptionFactory.CombineUnit(dirtyMeta.CleanMetas.SelectMany(a => a.PropertyRoutes).ToArray()); 
            }

            result.IsAllowed = () => me?.Meta.IsAllowed();

            if (ForcePropertyRoute != null)
                result.PropertyRoute = ForcePropertyRoute.Value;

            if (ForceImplementations != null)
                result.Implementations = ForceImplementations.Value;

            if (ForceFormat != null)
                result.Format = ForceFormat.Value;

            if (ForceUnit != null)
                result.Unit = ForceUnit.Value;

            if (ForceIsAllowed != null)
                result.IsAllowed = ForceIsAllowed!;

            return result;
        });

        return new ExtensionToken(parent, Key, Type, IsProjection, info.Unit, info.Format,
            info.Implementations, info.IsAllowed, info.PropertyRoute, displayName: NiceName, AutoExpand);
    }
}

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class ExtensionRouteInfo
{
    public string? Format;
    public string? Unit;
    public Implementations? Implementations;
    public Func<string?> IsAllowed;
    public PropertyRoute? PropertyRoute;
}
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
