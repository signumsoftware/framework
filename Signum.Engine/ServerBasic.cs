using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using System.ServiceModel;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Engine.Linq;
using Signum.Engine.Operations;
using Signum.Engine.Basics;
using System.ServiceModel.Channels;
using Signum.Entities.Reflection;
using System.Threading;

namespace Signum.Services
{
    public abstract class ServerBasic : IBaseServer, IDynamicQueryServer, IOperationServer
    {
        protected Dictionary<string, object> session = new Dictionary<string, object>();

        protected T Return<T>(MethodBase mi, Func<T> function)
        {
            return Return(mi, null, function);
        }

        protected virtual T Return<T>(MethodBase mi, string description, Func<T> function)
        {
            try
            {
                using (CultureFromOperationContext())
                using (ScopeSessionFactory.OverrideSession(session))
                using (ExecutionMode.Global())
                {
                    return function();
                }
            }
            catch (Exception e)
            {
                e.LogException(el =>
                {
                    el.ControllerName = GetType().Name;
                    el.ActionName = mi.Name;
                    el.QueryString = description;
                });

                throw;
            }
            finally
            {
                Statics.CleanThreadContextAndAssert();
            }
        }

        protected void Execute(MethodBase mi, Action action)
        {
            Return(mi, null, () => { action(); return true; });
        }

        protected void Execute(MethodBase mi, string description, Action action)
        {
            Return(mi, description, () => { action(); return true; });
        }

        public static IDisposable CultureFromOperationContext()
        {
            MessageHeaders headers = OperationContext.Current.IncomingMessageHeaders;

            int culture = headers.FindHeader("CurrentCulture", "http://www.signumsoftware.com/Culture");
            int cultureUI = headers.FindHeader("CurrentUICulture", "http://www.signumsoftware.com/Culture");

            var changeCulture = culture == -1 ? null : CultureInfoUtils.ChangeCulture(headers.GetHeader<string>(culture));
            var changeUICulture = cultureUI == -1 ? null : CultureInfoUtils.ChangeCulture(headers.GetHeader<string>(cultureUI));

            return Disposable.Combine(changeCulture, changeUICulture);
        }

        #region IBaseServer
        public virtual Entity Retrieve(Type type, PrimaryKey id)
        {
            return Return(MethodInfo.GetCurrentMethod(), type.Name,
                () => Database.Retrieve(type, id));
        }

        public virtual Entity Save(Entity entidad)
        {
            return Return(MethodInfo.GetCurrentMethod(), entidad.GetType().Name,
                () => Database.Save(entidad));
        }

        public virtual List<Entity> RetrieveAll(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(), type.Name,
                () => Database.RetrieveAll(type));
        }

        public virtual List<Lite<Entity>> RetrieveAllLite(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(), type.Name,
                () => Database.RetrieveAllLite(type));
        }

        public virtual List<Entity> SaveList(List<Entity> list)
        {
            Execute(MethodInfo.GetCurrentMethod(),
                () =>Database.SaveList(list));
            return list;
        }

        public virtual List<Lite<Entity>> FindAllLite(Implementations implementations)
        {
            return Return(MethodInfo.GetCurrentMethod(), implementations.ToString(),
                () => AutocompleteUtils.FindAllLite(implementations));
        }

        public virtual List<Lite<Entity>> FindLiteLike(Implementations implementations, string subString, int count)
        {
            return Return(MethodInfo.GetCurrentMethod(), implementations.ToString(),
                () => AutocompleteUtils.FindLiteLike(implementations, subString, count));
        }

        public virtual Dictionary<PropertyRoute, Implementations> FindAllImplementations(Type root)
        {
            return Return(MethodInfo.GetCurrentMethod(), root.Name,
                () => Schema.Current.FindAllImplementations(root));
        }

        public virtual Dictionary<Type, HashSet<Type>> FindAllMixins()
        {
            return Return(MethodInfo.GetCurrentMethod(), null,
             () => MixinDeclarations.Declarations.ToDictionary());
        }

        public virtual bool Exists(Type type, PrimaryKey id)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                  () => Database.Exists(type, id));
        }

        public virtual Dictionary<Type, TypeEntity> ServerTypes()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => TypeLogic.TypeToEntity);
        }

        public virtual DateTime ServerNow()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => TimeZoneManager.Now);
        }

        public virtual string GetToStr(Type type, PrimaryKey id)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => Database.GetToStr(type, id));
        }

        public virtual long Ticks(Lite<Entity> entity)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => entity.InDB(e => e.Ticks));
        }

        public Dictionary<string, PrimaryKey> GetSymbolIds(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                 () => Symbol.GetSymbolIds(type));
        }

        public Dictionary<string, SemiSymbol> GetSemiSymbolFromDatabase(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                 () => SemiSymbol.GetFromDatabase(type));
        }

        public Dictionary<Type, Type> ImportPrimaryKeyDefinitions()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                 () => PrimaryKey.Export());
        }
        #endregion

        #region IDynamicQueryServer
        public virtual QueryDescription GetQueryDescription(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => QueryLogic.Queries.QueryDescription(queryName));
        }

        public ResultTable ExecuteQuery(QueryRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(), request.QueryName.ToString(),
                () => QueryLogic.Queries.ExecuteQuery(request));
        }
        
        public virtual int ExecuteQueryCount(QueryValueRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(), request.QueryName.ToString(),
                () => (int)QueryLogic.Queries.ExecuteQueryValue(request));
        }

        public virtual Lite<Entity> ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(), request.QueryName.ToString(),
                () => QueryLogic.Queries.ExecuteUniqueEntity(request));
        }

        public virtual List<object> GetQueryNames()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => QueryLogic.Queries.GetQueryNames());
        }

        public virtual List<QueryToken> ExternalQueryToken(QueryToken parent)
        {
            return Return(MethodInfo.GetCurrentMethod(),
               () => QueryLogic.Expressions.GetExtensions(parent).ToList());
        }

        public virtual object[] BatchExecute(BaseQueryRequest[] requests)
        {
            return Return(MethodInfo.GetCurrentMethod(), requests.ToString("; "),
                () => QueryLogic.Queries.BatchExecute(requests, CancellationToken.None).Result);
        }
        #endregion

        #region IOperationServer Members
        public Dictionary<OperationSymbol, string> GetCanExecuteAll(Entity entity)
        {
            return Return(MethodInfo.GetCurrentMethod(), entity.GetType().Name,
                () => OperationLogic.ServiceCanExecute(entity));
        }

        public Dictionary<OperationSymbol, string> GetCanExecuteLiteAll(Lite<Entity> lite)
        {
            return Return(MethodInfo.GetCurrentMethod(), lite.EntityType.Name,
                () => OperationLogic.ServiceCanExecute(Database.Retrieve(lite)));
        }

        public string GetCanExecute(Entity entity, OperationSymbol operationSymbol)
        {
            return Return(MethodInfo.GetCurrentMethod(), entity.GetType().Name + " " + operationSymbol,
                () => OperationLogic.ServiceCanExecute(entity, operationSymbol));
        }

        public string GetCanExecuteLite(Lite<Entity> lite, OperationSymbol operationSymbol)
        {
            return Return(MethodInfo.GetCurrentMethod(), lite.EntityType.Name + " " + operationSymbol,
                () => OperationLogic.ServiceCanExecute(lite.Retrieve(), operationSymbol));
        }

        public List<OperationInfo> GetOperationInfos(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => OperationLogic.ServiceGetOperationInfos(entityType));
        }

        public bool HasConstructOperations(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => OperationLogic.HasConstructOperations(entityType));
        }

        public Entity ExecuteOperation(IEntity entity, OperationSymbol operationSymbol, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationSymbol.ToString(),
                () => OperationLogic.ServiceExecute(entity, operationSymbol, args));
        }

        public Entity ExecuteOperationLite(Lite<IEntity> lite, OperationSymbol operationSymbol, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationSymbol.ToString(),
                () => (Entity)OperationLogic.ServiceExecuteLite(lite, operationSymbol, args));
        }

        public void DeleteLite(Lite<IEntity> lite, OperationSymbol operationSymbol, params object[] args)
        {
            Execute(MethodInfo.GetCurrentMethod(), operationSymbol.ToString(),
                 () => OperationLogic.ServiceDelete(lite, operationSymbol, args));
        }

        public void Delete(IEntity entity, OperationSymbol operationSymbol, params object[] args)
        {
            Execute(MethodInfo.GetCurrentMethod(), operationSymbol.ToString(),
                 () => OperationLogic.ServiceDelete((Entity)entity, operationSymbol, args));
        }

        public Entity Construct(Type type, OperationSymbol operationSymbol, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationSymbol.ToString(),
                () => OperationLogic.ServiceConstruct(type, operationSymbol, args));
        }

        public Entity ConstructFrom(IEntity entity, OperationSymbol operationSymbol, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationSymbol.ToString(),
                () => OperationLogic.ServiceConstructFrom(entity, operationSymbol, args));
        }

        public Entity ConstructFromLite(Lite<IEntity> lite, OperationSymbol operationSymbol, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationSymbol.ToString(),
                () => OperationLogic.ServiceConstructFromLite(lite, operationSymbol, args));
        }

        public Entity ConstructFromMany(IEnumerable<Lite<IEntity>> lites, Type type, OperationSymbol operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationKey.ToString(),
                () => OperationLogic.ServiceConstructFromMany(lites, type, operationKey, args));
        }

        public Dictionary<OperationSymbol, string> GetContextualCanExecute(IEnumerable<Lite<IEntity>> lite, List<OperationSymbol> operatonSymbols)
        {
            return Return(MethodInfo.GetCurrentMethod(), null,
                () => OperationLogic.GetContextualCanExecute(lite, operatonSymbols));
        }

        #endregion





    }
}
