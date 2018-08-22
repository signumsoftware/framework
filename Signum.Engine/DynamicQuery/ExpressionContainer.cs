using Signum.Engine.Linq;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.DynamicQuery
{
    public class ExpressionContainer
    {
        public Polymorphic<Dictionary<string, ExtensionInfo>> RegisteredExtensions =
            new Polymorphic<Dictionary<string, ExtensionInfo>>(PolymorphicMerger.InheritDictionaryInterfaces, null);

        public Dictionary<PropertyRoute, IExtensionDictionaryInfo> RegisteredExtensionsDictionaries =
            new Dictionary<PropertyRoute, IExtensionDictionaryInfo>();

        internal Expression BuildExtension(Type parentType, string key, Expression parentExpression)
        {
            LambdaExpression lambda = RegisteredExtensions.GetValue(parentType)[key].Lambda;

            return ExpressionReplacer.Replace(Expression.Invoke(lambda, parentExpression));
        }

        public IEnumerable<QueryToken> GetExtensions(QueryToken parent)
        {
            var parentType = parent.Type.CleanType().UnNullify();

            var dic = RegisteredExtensions.TryGetValue(parentType);

            IEnumerable<QueryToken> extensionsTokens = dic == null ? Enumerable.Empty<QueryToken>() :
                dic.Values.Where(ei => ei.Inherit || ei.SourceType == parentType).Select(v => v.CreateToken(parent));

            var pr = parentType.IsEntity() && !parentType.IsAbstract ? PropertyRoute.Root(parentType) :
                parentType.IsEmbeddedEntity() ? parent.GetPropertyRoute() : null;

            var edi = pr == null ? null : RegisteredExtensionsDictionaries.TryGetC(pr);

            IEnumerable<QueryToken> dicExtensionsTokens = edi == null ? Enumerable.Empty<QueryToken>() :
                edi.GetAllTokens(parent);

            return extensionsTokens.Concat(dicExtensionsTokens);
        }

        public ExtensionInfo Register<E, S>(Expression<Func<E, S>> lambdaToMethodOrProperty, Func<string> niceName = null)
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
            if (mi.DeclaringType.Assembly == typeof(Enumerable).Assembly ||
                mi.DeclaringType.Assembly == typeof(Csv).Assembly ||
                mi.DeclaringType.Assembly == typeof(Lite).Assembly)
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



        public ExtensionDictionaryInfo<T, KVP, K, V> RegisterDictionary<T, KVP, K, V>(
            Expression<Func<T, IEnumerable<KVP>>> collectionSelector,
            Expression<Func<KVP, K>> keySelector,
            Expression<Func<KVP, V>> valueSelector,
            Expression<Func<T, EmbeddedEntity>> forEmbedded = null,
            ResetLazy<HashSet<K>> allKeys = null)
            where T : Entity
        {
            var mei = new ExtensionDictionaryInfo<T, KVP, K, V>
            {
                CollectionSelector = collectionSelector,
                KeySelector = keySelector,
                ValueSelector = valueSelector,

                AllKeys = allKeys ?? GetAllKeysLazy<T, KVP, K>(collectionSelector, keySelector)
            };

            var route = forEmbedded == null ?
                PropertyRoute.Root(typeof(T)) :
                PropertyRoute.Construct(forEmbedded);

            RegisteredExtensionsDictionaries.Add(route, mei);

            return mei;
        }

        private ResetLazy<HashSet<K>> GetAllKeysLazy<T, KVP, K>(Expression<Func<T, IEnumerable<KVP>>> collectionSelector, Expression<Func<KVP, K>> keySelector)
            where T : Entity
        {
            if (typeof(K).IsEnum)
                return new ResetLazy<HashSet<K>>(() => EnumExtensions.GetValues<K>().ToHashSet());

            if (typeof(K).IsLite())
                return GlobalLazy.WithoutInvalidations(() => Database.RetrieveAllLite(typeof(K).CleanType()).Cast<K>().ToHashSet());

            if (collectionSelector.Body.Type.IsMList())
            {
                var lambda = Expression.Lambda<Func<T, MList<KVP>>>(collectionSelector.Body, collectionSelector.Parameters);

                return GlobalLazy.WithoutInvalidations(() => Database.MListQuery(lambda).Select(kvp => keySelector.Evaluate(kvp.Element)).Distinct().ToHashSet());
            }
            else
            {
                return GlobalLazy.WithoutInvalidations(() => Database.Query<T>().SelectMany(collectionSelector).Select(keySelector).Distinct().ToHashSet());
            }
        }
    }

    public interface IExtensionDictionaryInfo
    {
        IEnumerable<QueryToken> GetAllTokens(QueryToken parent);
    }

    public class ExtensionDictionaryInfo<T, KVP, K, V> : IExtensionDictionaryInfo
    {
        public ResetLazy<HashSet<K>> AllKeys;

        public Expression<Func<T, IEnumerable<KVP>>> CollectionSelector { get; set; }

        public Expression<Func<KVP, K>> KeySelector { get; set; }

        public Expression<Func<KVP, V>> ValueSelector { get; set; }

        ConcurrentDictionary<QueryToken, ExtensionRouteInfo> metas = new ConcurrentDictionary<QueryToken, ExtensionRouteInfo>();

        public IEnumerable<QueryToken> GetAllTokens(QueryToken parent)
        {
            var info = metas.GetOrAdd(parent, qt =>
            {
                Expression<Func<T, V>> lambda = t => ValueSelector.Evaluate(CollectionSelector.Evaluate(t).SingleOrDefaultEx());

                Expression e = MetadataVisitor.JustVisit(lambda, MetaExpression.FromToken(qt, typeof(T)));

                MetaExpression me = e as MetaExpression;

                var result = new ExtensionRouteInfo();

                if (me?.Meta is CleanMeta cm && cm.PropertyRoutes.Any())
                {
                    var cleanType = me.Type.CleanType();

                    result.PropertyRoute = cm.PropertyRoutes.Only();
                    result.Implementations = me.Meta.Implementations;
                    result.Format = ColumnDescriptionFactory.GetFormat(cm.PropertyRoutes);
                    result.Unit = ColumnDescriptionFactory.GetUnit(cm.PropertyRoutes);
                }

                return result;
            });

            return AllKeys.Value.Select(key => new ExtensionDictionaryToken<T, K, V>(parent,
                key: key,
                unit: info.Unit,
                format: info.Format,
                implementations: info.Implementations,
                propertyRoute: info.PropertyRoute)
            {
                Lambda = t => ValueSelector.Evaluate(CollectionSelector.Evaluate(t).SingleOrDefaultEx(kvp => KeySelector.Evaluate(kvp).Equals(key))),
            });
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
        public bool IsProjection;
        public bool Inherit = true;

        public Implementations? ForceImplementations;
        public PropertyRoute ForcePropertyRoute;
        public string ForceFormat;
        public string ForceUnit;
        public Func<string> ForceIsAllowed;


        internal readonly LambdaExpression Lambda;
        public Func<string> NiceName;

        protected internal virtual ExtensionToken CreateToken(QueryToken parent)
        {
            var info = metas.GetOrAdd(parent, qt =>
            {
                Expression e = MetadataVisitor.JustVisit(Lambda, MetaExpression.FromToken(qt, SourceType));

                MetaExpression me;

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

                if (me?.Meta is CleanMeta cm && cm.PropertyRoutes.Any())
                {
                    var cleanType = me.Type.CleanType();

                    result.PropertyRoute = cm.PropertyRoutes.Only();
                    result.Implementations = me.Meta.Implementations;
                    result.Format = ColumnDescriptionFactory.GetFormat(cm.PropertyRoutes);
                    result.Unit = ColumnDescriptionFactory.GetUnit(cm.PropertyRoutes);
                }

                result.IsAllowed = () => (me == null || me.Meta == null) ? null : me.Meta.IsAllowed();

                if (ForcePropertyRoute != null)
                    result.PropertyRoute = ForcePropertyRoute;

                if (ForceImplementations != null)
                    result.Implementations = ForceImplementations;

                if (ForceFormat != null)
                    result.Format = ForceFormat;

                if (ForceUnit != null)
                    result.Unit = ForceUnit;

                if (ForceIsAllowed != null)
                    result.IsAllowed = ForceIsAllowed;

                return result;
            });

            return new ExtensionToken(parent, Key, Type, IsProjection, info.Unit, info.Format, info.Implementations, info.IsAllowed(), info.PropertyRoute)
            {
                DisplayName = NiceName()
            };
        }
    }

    public class ExtensionRouteInfo
    {
        public string Format;
        public string Unit;
        public Implementations? Implementations;
        public Func<string> IsAllowed;
        public PropertyRoute PropertyRoute;
    }
}
