using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Data;
using System.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Properties;
using Signum.Entities;
using System.Diagnostics;
using Signum.Engine.Maps;
using Signum.Services;
using System.Linq.Expressions;
using Signum.Engine.Linq;
using Signum.Entities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Engine.DynamicQuery
{
    public class DynamicQueryManager
    {
        public static DynamicQueryManager Current
        {
            get { return Connector.Current.DynamicQueryManager; }

        }

        Dictionary<object, IDynamicQuery> queries = new Dictionary<object, IDynamicQuery>();

        Polymorphic<Dictionary<string, ExtensionInfo>> registeredExtensions =
            new Polymorphic<Dictionary<string, ExtensionInfo>>(PolymorphicMerger.InheritDictionaryInterfaces,
            typeof(IIdentifiable)); 

        public IDynamicQuery this[object queryName]
        {
            get
            {
                AssertQueryAllowed(queryName);
                return queries.GetOrThrow(queryName, "The query {0} is not on registered");
            }
            set
            {
                queries[queryName] = value;
            }
        }

        IDynamicQuery TryGet(object queryName)
        {
            AssertQueryAllowed(queryName); 
            return queries.TryGetC(queryName);
        }

        public ResultTable ExecuteQuery(QueryRequest request)
        {
            return this[request.QueryName].ExecuteQuery(request);
        }

        public int ExecuteQueryCount(QueryCountRequest request)
        {
            return this[request.QueryName].ExecuteQueryCount(request);
        }

        public Lite ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            return this[request.QueryName].ExecuteUniqueEntity(request);
        }

        public QueryDescription QueryDescription(object queryName)
        {
            return this[queryName].GetDescription(queryName);
        }

        public event Func<object, bool> AllowQuery;

        public bool QueryAllowed(object queryName)
        {
            if (AllowQuery == null)
                return true;

            return AllowQuery(queryName);
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
                throw new UnauthorizedAccessException("Access to query {0} not allowed".Formato(queryName));
        }

        public List<object> GetAllowedQueryNames()
        {
            return queries.Keys.Where(QueryAllowed).ToList();
        }

        public List<object> GetQueryNames()
        {
            return queries.Keys.ToList();
        }

        public Dictionary<object, IDynamicQuery> GetQueries(Type entityType)
        {
            return queries.Where(kvp => kvp.Value.EntityColumn().CompatibleWith(entityType)).ToDictionary();
        }

        public Dictionary<object, IDynamicQuery> GetQueries()
        {
            return queries.ToDictionary();
        }

        static DynamicQueryManager()
        {
            QueryToken.EntityExtensions = (entityType, parent) => DynamicQueryManager.Current.GetExtensions(entityType, parent);
            ExtensionToken.BuildExtension = (entityType, key, expression) => DynamicQueryManager.Current.BuildExtension(entityType, key, expression);
        }

        private Expression BuildExtension(Type entityType, string key, Expression context)
        {
            LambdaExpression lambda = registeredExtensions.GetValue(entityType)[key].Lambda;

            return ExpressionReplacer.Replace(Expression.Invoke(lambda, context));
        }

        public IEnumerable<QueryToken> GetExtensions(Type entityType, QueryToken parent)
        {
            var dic = registeredExtensions.TryGetValue(entityType);
            
            if (dic == null)
                return Enumerable.Empty<QueryToken>();

            return dic.Values.Where(a => a.Inherit || a.EntityType == entityType).Select(v => v.CreateToken(parent));
        }

        public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> lambdaToMethodOrProperty)
            where E : class, IIdentifiable
        {
            if (lambdaToMethodOrProperty.Body.NodeType == ExpressionType.Call)
            {
                var mi = ReflectionTools.GetMethodInfo(lambdaToMethodOrProperty);

                AssertExtensionMethod(mi);

                return RegisterExpression<E, S>(lambdaToMethodOrProperty, () => mi.Name.NiceName(), mi.Name);
            }
            else if (lambdaToMethodOrProperty.Body.NodeType == ExpressionType.MemberAccess)
            {
                var pi = ReflectionTools.GetPropertyInfo(lambdaToMethodOrProperty);

                return RegisterExpression<E, S>(lambdaToMethodOrProperty, () => pi.NiceName(), pi.Name);
            }
            else throw new InvalidOperationException("argument 'lambdaToMethodOrProperty' should be a simple lambda calling a method or property: {0}".Formato(lambdaToMethodOrProperty.NiceToString()));
        }

        public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> lambdaToMethodOrProperty, Func<string> niceName)
           where E : class, IIdentifiable
        {
            if (lambdaToMethodOrProperty.Body.NodeType == ExpressionType.Call)
            {
                var mi = ReflectionTools.GetMethodInfo(lambdaToMethodOrProperty);

                AssertExtensionMethod(mi);

                return RegisterExpression<E, S>(lambdaToMethodOrProperty, niceName, mi.Name);
            }
            else if (lambdaToMethodOrProperty.Body.NodeType == ExpressionType.MemberAccess)
            {
                var pi = ReflectionTools.GetPropertyInfo(lambdaToMethodOrProperty);

                return RegisterExpression<E, S>(lambdaToMethodOrProperty, niceName, pi.Name);
            }
            else throw new InvalidOperationException("argument 'lambdaToMethodOrProperty' should be a simple lambda calling a method or property: {0}".Formato(lambdaToMethodOrProperty.NiceToString()));
        }

        private static void AssertExtensionMethod(MethodInfo mi)
        {
            if (mi.DeclaringType.Assembly == typeof(Enumerable).Assembly ||
                mi.DeclaringType.Assembly == typeof(Csv).Assembly ||
                mi.DeclaringType.Assembly == typeof(Lite).Assembly ||
                mi.DeclaringType.Assembly == typeof(Database).Assembly)
                throw new InvalidOperationException("The parameter 'lambdaToMethod' should be an expression calling a expression method");
        }

        public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> extensionLambda, Func<string> niceName, string key) 
            where E : class, IIdentifiable
        {
            var extension = new ExtensionInfo(typeof(S), key, extensionLambda, typeof(E))
            {   
                NiceName = niceName,
            };

            return RegisterExpression(extension);
        }

        private ExtensionInfo RegisterExpression(ExtensionInfo extension)
        {
            registeredExtensions.GetOrAdd(extension.EntityType)[extension.Key] = extension;

            registeredExtensions.ClearCache();

            return extension;
        }

        internal object[] BatchExecute(BaseQueryRequest[] requests)
        {
            return requests.Select(r =>
            {
                if (r is QueryCountRequest)
                    return ExecuteQueryCount((QueryCountRequest)r);

                if (r is QueryRequest)
                    return ExecuteQuery((QueryRequest)r);

                if (r is UniqueEntityRequest)
                    return ExecuteUniqueEntity((UniqueEntityRequest)r);

                return (object)null;
            }).ToArray(); 
        }
    }

    public class ExtensionInfo
    {
        public ExtensionInfo(Type type, string key, LambdaExpression lambda, Type entityType)
        {
            this.Type = type;
            this.EntityType = entityType;
            this.Key = key;
            this.Lambda = lambda;

            Expression e = MetadataVisitor.JustVisit(lambda, entityType);

            if (e is MetaProjectorExpression)
                e = ((MetaProjectorExpression)e).Projector;

            MetaExpression me = e as MetaExpression;
            CleanMeta cm = me == null ? null : me.Meta as CleanMeta;

            if (cm != null)
            {
                this.Format = ColumnDescriptionFactory.GetFormat(cm.PropertyRoutes);
                this.Unit = ColumnDescriptionFactory.GetUnit(cm.PropertyRoutes);
                this.Implementations = ColumnDescriptionFactory.AggregateImplementations(cm.PropertyRoutes, type.CleanType());
                this.PropertyRoute = cm.PropertyRoutes.FirstOrDefault();
            }

            IsAllowed = () => me == null || me.Meta == null || me.Meta.IsAllowed();
        }

        public readonly Type Type;
        public readonly Type EntityType;
        public readonly string Key; 

        internal readonly LambdaExpression Lambda;
        public Func<string> NiceName;
        public string Format;
        public string Unit;
        public Implementations Implementations;
        public Func<bool> IsAllowed;
        public PropertyRoute PropertyRoute;
        public bool Inherit = true;

        protected internal virtual ExtensionToken CreateToken(QueryToken parent)
        {
            return new ExtensionToken(parent, Key, Type, Unit, Format, Implementations, IsAllowed(), PropertyRoute) { DisplayName = NiceName() }; 
        }

       
    }
}
