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

     

        public void RegisterQuery<T>(object queryName, Func<IQueryable<T>> lazyQuery, Implementations? entityImplementations = null)
        {
            queries[queryName] = new DynamicQueryBucket(queryName, () => DynamicQueryCore.Auto(lazyQuery()), entityImplementations ?? DefaultImplementations(typeof(T), queryName));
        }

        public void RegisterQuery<T>(object queryName, Func<DynamicQueryCore<T>> lazyQueryCore, Implementations? entityImplementations = null)
        {
            queries[queryName] = new DynamicQueryBucket(queryName, lazyQueryCore, entityImplementations ?? DefaultImplementations(typeof(T), queryName));
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
            AssertQueryAllowed(queryName); 
            return queries.TryGetC(queryName);
        }

        public DynamicQueryBucket GetQuery(object queryName)
        {
            AssertQueryAllowed(queryName);
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
            return Execute(ExecuteType.ExecuteQuery, request.QueryName,request, dqb => dqb.Core.Value.ExecuteQuery(request));
        }

        public object ExecuteQueryCount(QueryValueRequest request)
        {
            return Execute(ExecuteType.ExecuteQueryCount, request.QueryName, request, dqb => dqb.Core.Value.ExecuteQueryValue(request));
        }

        public ResultTable ExecuteGroupQuery(QueryGroupRequest request)
        {
            return Execute(ExecuteType.ExecuteGroupQuery, request.QueryName,request, dqb => dqb.Core.Value.ExecuteQueryGroup(request));
        }

        public Lite<Entity> ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            return Execute(ExecuteType.ExecuteUniqueEntity, request.QueryName,request, dqb => dqb.Core.Value.ExecuteUniqueEntity(request));
        }

        public QueryDescription QueryDescription(object queryName)
        {
            return Execute(ExecuteType.QueryDescription, queryName, null, dqb => dqb.GetDescription());
        }

        public IQueryable<Lite<Entity>> GetEntities(QueryEntitiesRequest request)
        {
            return Execute(ExecuteType.GetEntities, request.QueryName, null, dqb => dqb.Core.Value.GetEntities(request));
        }

        public event Func<object, bool> AllowQuery;

        public bool QueryAllowed(object queryName)
        {
            foreach (var f in AllowQuery.GetInvocationListTyped())
            {
                if (!f(queryName))
                    return false;
            }

            return true;
        }

        public bool QueryDefined(object queryName)
        {
            return this.queries.ContainsKey(queryName);
        }

        public bool QueryDefinedAndAllowed(object queryName)
        {
            return QueryDefined(queryName) && QueryAllowed(queryName);
        }

        public void AssertQueryAllowed(object queryName)
        {
            if(!QueryAllowed(queryName))
                throw new UnauthorizedAccessException("Access to query {0} not allowed".FormatWith(queryName));
        }

        public List<object> GetAllowedQueryNames()
        {
            return queries.Keys.Where(QueryAllowed).ToList();
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
            
            if (dic == null)
                return Enumerable.Empty<QueryToken>();

            return dic.Values.Where(a => a.Inherit || a.SourceType == parentType).Select(v => v.CreateToken(parent));
        }


        public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> lambdaToMethodOrProperty, Func<string> niceName = null)
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

        private static void AssertExtensionMethod(MethodInfo mi)
        {
            if (mi.DeclaringType.Assembly == typeof(Enumerable).Assembly ||
                mi.DeclaringType.Assembly == typeof(Csv).Assembly ||
                mi.DeclaringType.Assembly == typeof(Lite).Assembly)
                throw new InvalidOperationException("The parameter 'lambdaToMethod' should be an expression calling a expression method");
        }

        public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> extensionLambda, Func<string> niceName, string key) 
        {
            var extension = new ExtensionInfo(typeof(E), extensionLambda, typeof(S), key, niceName);

            return RegisterExpression(extension);
        }

        public ExtensionInfo RegisterExpression(ExtensionInfo extension)
        {
            RegisteredExtensions.GetOrAddDefinition(extension.SourceType)[extension.Key] = extension;

            RegisteredExtensions.ClearCache();

            return extension;
        }

        public object[] BatchExecute(BaseQueryRequest[] requests)
        {
            return requests.Select(r =>
            {
                if (r is QueryValueRequest)
                    return ExecuteQueryCount((QueryValueRequest)r);

                if (r is QueryRequest)
                    return ExecuteQuery((QueryRequest)r);

                if (r is UniqueEntityRequest)
                    return ExecuteUniqueEntity((UniqueEntityRequest)r);

                if (r is QueryGroupRequest)
                    return ExecuteGroupQuery((QueryGroupRequest)r);

                return (object)null;
            }).ToArray(); 
        }
    }

    public static class DynamicQueryFluentInclude
    {
        public static FluentInclude<T> WithQuery<T, Q>(this FluentInclude<T> fi, DynamicQueryManager dqm, Expression<Func<T, Q>> simpleQuerySelector)
            where T : Entity
        {
            dqm.RegisterQuery<Q>(typeof(T), () => Database.Query<T>().Select(simpleQuerySelector));
            return fi;
        }

        public static FluentInclude<T> WithQuery<T, Q>(this FluentInclude<T> fi, DynamicQueryManager dqm, Expression<Func<T, Q>> simpleQuerySelector, Action<AutoDynamicQueryCore<Q>> modifyQuery)
            where T : Entity
        {
            dqm.RegisterQuery<Q>(typeof(T), () =>
            {
                var autoQuery = DynamicQueryCore.Auto(Database.Query<T>().Select(simpleQuerySelector));
                modifyQuery(autoQuery);
                return autoQuery;
            });
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
    }
        

    public class ExtensionInfo
    {
        public class ExtensionRouteInfo
        {
            public string Format;
            public string Unit;
            public Implementations? Implementations;
            public Func<string> IsAllowed;
            public PropertyRoute PropertyRoute;
        }

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

                }else 
                {
                    me = e as MetaExpression;
                }
     
                CleanMeta cm = me == null ? null : me.Meta as CleanMeta;

                var result = new ExtensionRouteInfo();

                if (cm != null && cm.PropertyRoutes.Any())
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
}
