using Microsoft.Identity.Client;
using Signum.Engine.Linq;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;

namespace Signum.Engine.DynamicQuery;

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
            edi.Values.SelectMany(e => e.GetAllTokens(parent));

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

                AssertExtensionMethod(mi);

                return Register<E, S>(lambdaToMethodOrProperty, niceName ?? (() => mi.Name.NiceName()), mi.Name);
            }
            else if (lambdaToMethodOrProperty.Body.NodeType == ExpressionType.MemberAccess)
            {
                var pi = ReflectionTools.GetPropertyInfo(lambdaToMethodOrProperty);

                return Register<E, S>(lambdaToMethodOrProperty, niceName ?? (() => pi.NiceName()), pi.Name);
            }
            else throw new InvalidOperationException("argument 'lambdaToMethodOrProperty' should be a simple lambda calling a method or property: {0}".FormatWith(lambdaToMethodOrProperty.ToString()));
        }
    }

    private static void AssertExtensionMethod(MethodInfo mi)
    {
        var assembly = mi.DeclaringType!.Assembly;

        if (assembly == typeof(Enumerable).Assembly ||
            assembly == typeof(Csv).Assembly ||
            assembly == typeof(Lite).Assembly)
            throw new InvalidOperationException("The parameter 'lambdaToMethod' should be an expression calling a expression method");
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
        Expression<Func<T, K, V>> extensionLambda,
        string prefix = "",
        Func<QueryToken, IEnumerable<K>>? getKeys = null)
        where K : notnull
    {
        if (getKeys == null)
        {
            var lazy = GetAllKeysLazy<K>();
            getKeys = t => lazy.Value;
        }

        var ei = new ExtensionWithParameterInfo<T, K, V>(extensionLambda, prefix, getKeys);

        RegisteredExtensionsWithParameter.GetOrCreate(typeof(T)).Add(ei.Prefix, ei);

        return ei;
    }


    private ResetLazy<HashSet<K>> GetAllKeysLazy<K>()
    {
        if (typeof(K).IsEnum)
            return new ResetLazy<HashSet<K>>(() => EnumExtensions.GetValues<K>().ToHashSet());

        if (typeof(K).IsLite())
            return GlobalLazy.WithoutInvalidations(() => Database.RetrieveAllLite(typeof(K).CleanType()).Cast<K>().ToHashSet());

        throw new InvalidOperationException("Unable to get all the possible keys for " + typeof(K).TypeName());
    }
}

public interface IExtensionDictionaryInfo
{
    IEnumerable<QueryToken> GetAllTokens(QueryToken parent);
}

public class ExtensionWithParameterInfo<T, K, V> : IExtensionDictionaryInfo
{
    public string Prefix; 
    public Func<QueryToken, IEnumerable<K>> AllKeys;

    public Expression<Func<T, K, V>> LambdaExpression { get; set; }

    readonly ConcurrentDictionary<QueryToken, ExtensionRouteInfo> metas = new ConcurrentDictionary<QueryToken, ExtensionRouteInfo>();

    public ExtensionWithParameterInfo(
        Expression<Func<T, K, V>> expression,
        string prefix,
        Func<QueryToken, IEnumerable<K>> allKeys)
    {
        LambdaExpression = expression;
        Prefix = prefix;
        AllKeys = allKeys;
    }

    public IEnumerable<QueryToken> GetAllTokens(QueryToken parent)
    {
        var info = metas.GetOrAdd(parent, qt =>
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
                result.Format = ColumnDescriptionFactory.GetFormat(cm.PropertyRoutes);
                result.Unit = ColumnDescriptionFactory.GetUnit(cm.PropertyRoutes);
            }

            return result;
        });

        return AllKeys(parent).Select(key => new ExtensionWithParameterToken<T, K, V>(parent,
            parameterValue: key,
            prefix: Prefix,
            unit: info.Unit,
            format: info.Format,
            implementations: info.Implementations,
            propertyRoute: info.PropertyRoute,
            lambda: t => LambdaExpression.Evaluate(t, key)
        ));
    }
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
    public bool IsProjection;
    public bool Inherit = true;

    public Implementations? ForceImplementations;
    public PropertyRoute? ForcePropertyRoute;
    public string? ForceFormat;
    public string? ForceUnit;
    public Func<string?>? ForceIsAllowed;


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
                result.Format = ColumnDescriptionFactory.GetFormat(cleanMeta.PropertyRoutes);
                result.Unit = ColumnDescriptionFactory.GetUnit(cleanMeta.PropertyRoutes);
            }
            else if (me?.Meta is DirtyMeta dirtyMeta)
            {
                result.PropertyRoute = dirtyMeta.CleanMetas.Select(cm => cm.PropertyRoutes.Only()).Distinct().Only();
                var metaImps = dirtyMeta.CleanMetas.Select(cm => cm.Implementations).Distinct().Only();
                if (metaImps.HasValue && !metaImps.Value.IsByAll && metaImps.Value.Types.All(t => t.IsAssignableFrom(Type)))
                {
                    result.Implementations = metaImps;
                }
                result.Format = dirtyMeta.CleanMetas.Select(cm => ColumnDescriptionFactory.GetFormat(cm.PropertyRoutes)).Distinct().Only();
                result.Unit = dirtyMeta.CleanMetas.Select(cm => ColumnDescriptionFactory.GetUnit(cm.PropertyRoutes)).Distinct().Only();
            }

            result.IsAllowed = () => me?.Meta.IsAllowed();

            if (ForcePropertyRoute != null)
                result.PropertyRoute = ForcePropertyRoute!;

            if (ForceImplementations != null)
                result.Implementations = ForceImplementations;

            if (ForceFormat != null)
                result.Format = ForceFormat;

            if (ForceUnit != null)
                result.Unit = ForceUnit;

            if (ForceIsAllowed != null)
                result.IsAllowed = ForceIsAllowed!;

            return result;
        });

        return new ExtensionToken(parent, Key, Type, IsProjection, info.Unit, info.Format,
            info.Implementations, info.IsAllowed, info.PropertyRoute, displayName: NiceName);
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
