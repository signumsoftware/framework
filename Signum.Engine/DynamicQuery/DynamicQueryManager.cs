using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Data;
using System.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Diagnostics;
using Signum.Engine.Maps;
using Signum.Services;
using System.Linq.Expressions;
using Signum.Engine.Linq;
using Signum.Entities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;
using System.Threading;
using static Signum.Engine.Maps.SchemaBuilder;
using System.Threading.Tasks;

namespace Signum.Engine.DynamicQuery
{
    public class DynamicQueryManager
    {
        public static DynamicQueryManager Current
        {
            get { return Connector.Current.DynamicQueryManager; }

        }

        Dictionary<object, DynamicQueryBucket> queries = new Dictionary<object, DynamicQueryBucket>();

        public Polymorphic<Dictionary<string, ExtensionInfo>> RegisteredExtensions =
            new Polymorphic<Dictionary<string, ExtensionInfo>>(PolymorphicMerger.InheritDictionaryInterfaces, null);

        public Dictionary<PropertyRoute, IExtensionDictionaryInfo> RegisteredExtensionsDictionaries =
            new Dictionary<PropertyRoute, IExtensionDictionaryInfo>();

        public void RegisterQuery<T>(object queryName, Func<DynamicQueryCore<T>> lazyQueryCore, Implementations? entityImplementations = null)
        {
            queries[queryName] = new DynamicQueryBucket(queryName, lazyQueryCore, entityImplementations ?? DefaultImplementations(typeof(T), queryName));
        }

        public void RegisterQuery<T>(object queryName, Func<IQueryable<T>> lazyQuery, Implementations? entityImplementations = null)
        {
            queries[queryName] = new DynamicQueryBucket(queryName, () => DynamicQueryCore.Auto(lazyQuery()), entityImplementations ?? DefaultImplementations(typeof(T), queryName));
        }
    
        public void RegisterQuery(object queryName, DynamicQueryBucket bucket)
        {
            queries[queryName] = bucket;
        }

        static Implementations DefaultImplementations(Type type, object queryName)
        {
            var property = type.GetProperty("Entity", BindingFlags.Instance | BindingFlags.Public);

            if (property == null)
                throw new InvalidOperationException("Entity property not found on query {0}".FormatWith(QueryUtils.GetKey(queryName)));

            return Implementations.By(property.PropertyType.CleanType());
        }

      

        public DynamicQueryBucket TryGetQuery(object queryName)
        {
            AssertQueryAllowed(queryName, false); 
            return queries.TryGetC(queryName);
        }

        public DynamicQueryBucket GetQuery(object queryName)
        {
            AssertQueryAllowed(queryName, false);
            return queries.GetOrThrow(queryName);
        }

        public Implementations GetEntityImplementations(object queryName)
        {
            //AssertQueryAllowed(queryName);
            return queries.GetOrThrow(queryName).EntityImplementations;
        }
        
        T Execute<T>(ExecuteType executeType, object queryName, BaseQueryRequest request, Func<DynamicQueryBucket, T> executor)
        {
            using (ExecutionMode.UserInterface())
            using (HeavyProfiler.Log(executeType.ToString(), () => QueryUtils.GetKey(queryName)))
            {
                try
                {
                    var qb = GetQuery(queryName);

                    using (Disposable.Combine(QueryExecuted, f => f(executeType, queryName, request)))
                    {
                        return executor(qb);
                    }
                }
                catch (Exception e)
                {
                    e.Data["QueryName"] = queryName;
                    throw;
                }
            }
        }

        async Task<T> ExecuteAsync<T>(ExecuteType executeType, object queryName, BaseQueryRequest request, Func<DynamicQueryBucket, Task<T>> executor)
        {
            using (ExecutionMode.UserInterface())
            using (HeavyProfiler.Log(executeType.ToString(), () => QueryUtils.GetKey(queryName)))
            {
                try
                {
                    var qb = GetQuery(queryName);

                    using (Disposable.Combine(QueryExecuted, f => f(executeType, queryName, request)))
                    {
                        return await executor(qb);
                    }
                }
                catch (Exception e)
                {
                    e.Data["QueryName"] = queryName;
                    throw;
                }
            }
        }

        public event Func<ExecuteType, object, BaseQueryRequest ,  IDisposable> QueryExecuted;

        public enum ExecuteType
        {
            ExecuteQuery,
            ExecuteQueryCount,
            ExecuteGroupQuery,
            ExecuteUniqueEntity,
            QueryDescription,
            GetEntities
        }

        public ResultTable ExecuteQuery(QueryRequest request)
        {
            if (!request.GroupResults)
                return Execute(ExecuteType.ExecuteGroupQuery, request.QueryName, request, dqb => dqb.Core.Value.ExecuteQueryGroup(request));
            else
                return Execute(ExecuteType.ExecuteQuery, request.QueryName, request, dqb => dqb.Core.Value.ExecuteQuery(request));
        }

        public Task<ResultTable> ExecuteQueryAsync(QueryRequest request, CancellationToken token)
        {
            if (!request.GroupResults)
                return ExecuteAsync(ExecuteType.ExecuteQuery, request.QueryName, request, dqb => dqb.Core.Value.ExecuteQueryAsync(request, token));
            else
                return ExecuteAsync(ExecuteType.ExecuteGroupQuery, request.QueryName, request, dqb => dqb.Core.Value.ExecuteQueryGroupAsync(request, token));
        }

        public object ExecuteQueryCount(QueryValueRequest request)
        {
            return Execute(ExecuteType.ExecuteQueryCount, request.QueryName, request, dqb => dqb.Core.Value.ExecuteQueryValue(request));
        }

        public Task<object> ExecuteQueryCountAsync(QueryValueRequest request, CancellationToken token)
        {
            return ExecuteAsync(ExecuteType.ExecuteQueryCount, request.QueryName, request, dqb => dqb.Core.Value.ExecuteQueryValueAsync(request, token));
        }       

        public Lite<Entity> ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            return Execute(ExecuteType.ExecuteUniqueEntity, request.QueryName, request, dqb => dqb.Core.Value.ExecuteUniqueEntity(request));
        }

        public Task<Lite<Entity>> ExecuteUniqueEntityAsync(UniqueEntityRequest request, CancellationToken token)
        {
            return ExecuteAsync(ExecuteType.ExecuteUniqueEntity, request.QueryName,request, dqb => dqb.Core.Value.ExecuteUniqueEntityAsync(request, token));
        }

        public QueryDescription QueryDescription(object queryName)
        {
            return Execute(ExecuteType.QueryDescription, queryName, null, dqb => dqb.GetDescription());
        }

        public IQueryable<Lite<Entity>> GetEntities(QueryEntitiesRequest request)
        {
            return Execute(ExecuteType.GetEntities, request.QueryName, null, dqb => dqb.Core.Value.GetEntities(request));
        }

        public event Func<object, bool, bool> AllowQuery;

        public bool QueryAllowed(object queryName, bool fullScreen)
        {
            foreach (var f in AllowQuery.GetInvocationListTyped())
            {
                if (!f(queryName, fullScreen))
                    return false;
            }

            return true;
        }

        public bool QueryDefined(object queryName)
        {
            return this.queries.ContainsKey(queryName);
        }

        public bool QueryDefinedAndAllowed(object queryName, bool fullScreen)
        {
            return QueryDefined(queryName) && QueryAllowed(queryName, fullScreen);
        }

        public void AssertQueryAllowed(object queryName, bool fullScreen)
        {
            if (!QueryAllowed(queryName, fullScreen))
                throw new UnauthorizedAccessException("Access to query {0} not allowed {1}".FormatWith(queryName, QueryAllowed(queryName, false) ? " for full screen" : ""));
        }

        public List<object> GetAllowedQueryNames(bool fullScreen)
        {
            return queries.Keys.Where(qn => QueryAllowed(qn, fullScreen)).ToList();
        }

        public Dictionary<object, DynamicQueryBucket> GetTypeQueries(Type entityType)
        {
            return (from kvp in queries
                    where !kvp.Value.EntityImplementations.IsByAll && kvp.Value.EntityImplementations.Types.Contains(entityType)
                    select kvp).ToDictionary();
        }

        public List<object> GetQueryNames()
        {
            return queries.Keys.ToList();
        }

        static DynamicQueryManager()
        {
            QueryToken.EntityExtensions = parent => DynamicQueryManager.Current.GetExtensions(parent);
            ExtensionToken.BuildExtension = (parentType, key, parentExpression) => DynamicQueryManager.Current.BuildExtension(parentType, key, parentExpression);
            QueryToken.ImplementedByAllSubTokens = GetImplementedByAllSubTokens;
            QueryToken.IsSystemVersioned = IsSystemVersioned;
        }

        static bool IsSystemVersioned(Type type)
        {
            var table = Schema.Current.Tables.TryGetC(type);
            return table != null && table.SystemVersioned != null;
        }

        static List<QueryToken> GetImplementedByAllSubTokens(QueryToken queryToken, Type type, SubTokensOptions options)
        {
            var cleanType = type.CleanType();
            return Schema.Current.Tables.Keys
                .Where(t => cleanType.IsAssignableFrom(t))
                .Select(t => (QueryToken)new AsTypeToken(queryToken, t))
                .ToList();
        }

        private Expression BuildExtension(Type parentType, string key, Expression parentExpression)
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
            
            var edi = pr == null ? null: RegisteredExtensionsDictionaries.TryGetC(pr);

            IEnumerable<QueryToken> dicExtensionsTokens = edi == null ? Enumerable.Empty<QueryToken>() :
                edi.GetAllTokens(parent);

            return extensionsTokens.Concat(dicExtensionsTokens);
        }
        
        public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> lambdaToMethodOrProperty, Func<string> niceName = null)
        {
            using (HeavyProfiler.LogNoStackTrace("RegisterExpression"))
            {
                if (lambdaToMethodOrProperty.Body.NodeType == ExpressionType.Call)
                {
                    var mi = ReflectionTools.GetMethodInfo(lambdaToMethodOrProperty);

                    AssertExtensionMethod(mi);

                    return RegisterExpression<E, S>(lambdaToMethodOrProperty, niceName ?? (() => mi.Name.NiceName()), mi.Name);
                }
                else if (lambdaToMethodOrProperty.Body.NodeType == ExpressionType.MemberAccess)
                {
                    var pi = ReflectionTools.GetPropertyInfo(lambdaToMethodOrProperty);

                    return RegisterExpression<E, S>(lambdaToMethodOrProperty, niceName ?? (() => pi.NiceName()), pi.Name);
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

        public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> extensionLambda, Func<string> niceName, string key, bool replace = false) 
        {
            var extension = new ExtensionInfo(typeof(E), extensionLambda, typeof(S), key, niceName);

            return RegisterExpression(extension);
        }

        public ExtensionInfo RegisterExpression(ExtensionInfo extension, bool replace = false)
        {
            var dic = RegisteredExtensions.GetOrAddDefinition(extension.SourceType);

            if (replace)
                dic[extension.Key] = extension;
            else
                dic.Add(extension.Key, extension);

            RegisteredExtensions.ClearCache();

            return extension;
        }

        

        public ExtensionDictionaryInfo<T, KVP, K, V> RegisterExpressionDictionary<T, KVP, K, V>(
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

        public Task<object[]> BatchExecute(BaseQueryRequest[] requests, CancellationToken token)
        {
            return Task.WhenAll<object>(requests.Select<BaseQueryRequest, Task<object>>(r =>
            {
                if (r is QueryValueRequest)
                    return ExecuteQueryCountAsync((QueryValueRequest)r, token);

                if (r is QueryRequest)
                    return ExecuteQueryAsync((QueryRequest)r, token).ContinueWith(a => (object)a.Result);

                if (r is UniqueEntityRequest)
                    return ExecuteUniqueEntityAsync((UniqueEntityRequest)r, token).ContinueWith(a => (object)a.Result);
                
                throw new InvalidOperationException("Unexpected QueryRequest type"); ;
            })); 
        }
    }


    public static class DynamicQueryFluentInclude
    {
        public static FluentInclude<T> WithQuery<T>(this FluentInclude<T> fi, DynamicQueryManager dqm, Func<Expression<Func<T, object>>> lazyQuerySelector)
            where T : Entity
        {
            dqm.RegisterQuery(typeof(T), new DynamicQueryBucket(typeof(T), () => DynamicQueryCore.FromSelectorUntyped(lazyQuerySelector()), Implementations.By(typeof(T))));
            return fi;
        }

        public static FluentInclude<T> WithQuery<T, Q>(this FluentInclude<T> fi, DynamicQueryManager dqm, Func<DynamicQueryCore<Q>> lazyGetQuery)
             where T : Entity
        {
            dqm.RegisterQuery<Q>(typeof(T), () => lazyGetQuery());
            return fi;
        }

        /// <summary>
        /// Uses NicePluralName as niceName
        /// </summary>
        public static FluentInclude<T> WithExpressionFrom<T, F>(this FluentInclude<T> fi, DynamicQueryManager dqm, Expression<Func<F, IQueryable<T>>> lambdaToMethodOrProperty)
            where T : Entity
        {
            dqm.RegisterExpression(lambdaToMethodOrProperty, () => typeof(T).NicePluralName());
            return fi;
        }

        public static FluentInclude<T> WithExpressionFrom<T, F>(this FluentInclude<T> fi, DynamicQueryManager dqm, Expression<Func<F, IQueryable<T>>> lambdaToMethodOrProperty, Func<string> niceName)
            where T : Entity
        {
            dqm.RegisterExpression(lambdaToMethodOrProperty, niceName);
            return fi;
        }

        /// <summary>
        /// Uses NicePluralName as niceName
        /// </summary>
        public static FluentInclude<T> WithExpressionFrom<T, F>(this FluentInclude<T> fi, DynamicQueryManager dqm, Expression<Func<F, T>> lambdaToMethodOrProperty)
            where T : Entity
        {
            dqm.RegisterExpression(lambdaToMethodOrProperty, () => typeof(T).NicePluralName());
            return fi;
        }

        public static FluentInclude<T> WithExpressionFrom<T, F>(this FluentInclude<T> fi, DynamicQueryManager dqm, Expression<Func<F, T>> lambdaToMethodOrProperty, Func<string> niceName)
            where T : Entity
        {
            dqm.RegisterExpression(lambdaToMethodOrProperty, niceName);
            return fi;
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

                    if(mpe == null)
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
