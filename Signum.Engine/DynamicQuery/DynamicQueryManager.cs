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
            get { return ConnectionScope.Current.DynamicQueryManager; }

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
            QueryToken.EntityExtensions = (type, parent) => DynamicQueryManager.Current.GetExtensions(type, parent);
            ExtensionToken.BuildExtension = (type, key, expression) => DynamicQueryManager.Current.BuildExtension(type, key, expression);
        }

        private Expression BuildExtension(Type type, string key, Expression context)
        {
            LambdaExpression lambda = registeredExtensions.GetValue(type)[key].Lambda;

            return ExpressionReplacer.Replace(Expression.Invoke(lambda, context));
        }

        public IEnumerable<QueryToken> GetExtensions(Type type, QueryToken parent)
        {
            var dic = registeredExtensions.TryGetValue(type);
            
            if (dic == null)
                return Enumerable.Empty<QueryToken>();

            return dic.Values.Select(v => v.CreateToken(parent));
        }

        public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> lambdaToMethod)
            where E : class, IIdentifiable
        {
            MethodInfo mi = ReflectionTools.GetMethodInfo(lambdaToMethod);

            AssertExtensionMethod(mi);

            return RegisterExpression<E, S>(lambdaToMethod, () => mi.Name.NiceName(), mi.Name);
        }

        public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> lambdaToMethod, Func<string> niceName)
            where E : class, IIdentifiable
        {
            MethodInfo mi = ReflectionTools.GetMethodInfo(lambdaToMethod);

            AssertExtensionMethod(mi);

            return RegisterExpression<E, S>(lambdaToMethod, niceName, mi.Name);
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
            var result = new ExtensionInfo(typeof(S), key, extensionLambda, typeof(E))
            {   
                NiceName = niceName,
            };

            registeredExtensions.GetOrAdd(typeof(E))[key] = result;

            registeredExtensions.ClearCache();

            return result;
        }
    }

    public class ExtensionInfo
    {
        public ExtensionInfo(Type type, string key, LambdaExpression lambda, Type entityType)
        {
            this.Type = type;
            this.Key = key;
            this.Lambda = lambda;

            Expression e = MetadataVisitor.JustVisit(lambda, entityType);

            MetaExpression me = e as MetaExpression ?? (e as MetaProjectorExpression).TryCC(a => a.Projector as MetaExpression);
            CleanMeta cm = me == null ? null : me.Meta as CleanMeta;

            if (cm != null)
            {
                this.Format = ColumnDescriptionFactory.GetFormat(cm.PropertyRoutes);
                this.Unit = ColumnDescriptionFactory.GetUnit(cm.PropertyRoutes);
                this.Implementations = ColumnDescriptionFactory.AggregateImplementations(cm.PropertyRoutes);
            }

            IsAllowed = () => me.Meta == null || me.Meta.IsAllowed();
        }

        public readonly Type Type;
        public readonly string Key; 

        internal readonly LambdaExpression Lambda;
        public Func<string> NiceName;
        public string Format;
        public string Unit;
        public Implementations Implementations;
        public Func<bool> IsAllowed;

        internal ExtensionToken CreateToken(QueryToken parent)
        {
            return new ExtensionToken(parent, Key, Type, Unit, Format, Implementations, IsAllowed()) { DisplayName = NiceName() }; 
        }
    }
}
