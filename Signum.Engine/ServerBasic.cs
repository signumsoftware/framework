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
                throw new FaultException(e.Message);
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

        #region IBaseServer
        public virtual IdentifiableEntity Retrieve(Type type, int id)
        {
            return Return(MethodInfo.GetCurrentMethod(), type.Name,
                () => Database.Retrieve(type, id));
        }

        public virtual IdentifiableEntity Save(IdentifiableEntity entidad)
        {
            return Return(MethodInfo.GetCurrentMethod(), entidad.GetType().Name,
                () => Database.Save(entidad));
        }

        public virtual List<IdentifiableEntity> RetrieveAll(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(), type.Name,
                () => Database.RetrieveAll(type));
        }

        public virtual List<Lite<IdentifiableEntity>> RetrieveAllLite(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(), type.Name,
                () => Database.RetrieveAllLite(type));
        }

        public virtual List<IdentifiableEntity> SaveList(List<IdentifiableEntity> list)
        {
            Execute(MethodInfo.GetCurrentMethod(),
                () =>Database.SaveList(list));
            return list;
        }

        public virtual List<Lite<IdentifiableEntity>> FindAllLite(Implementations implementations)
        {
            return Return(MethodInfo.GetCurrentMethod(), implementations.ToString(),
                () => AutocompleteUtils.FindAllLite(implementations));
        }

        public virtual List<Lite<IdentifiableEntity>> FindLiteLike(Implementations implementations, string subString, int count)
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
             () => MixinDeclarations.Declarations);
        }

        public virtual bool Exists(Type type, int id)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                  () => Database.Exists(type, id));
        }

        public virtual Dictionary<Type, TypeDN> ServerTypes()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => TypeLogic.TypeToDN);
        }

        public virtual DateTime ServerNow()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => TimeZoneManager.Now);
        }

        public virtual string GetToStr(Type type, int id)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => Database.GetToStr(type, id));
        }

        public virtual long Ticks(Lite<Entity> entity)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => entity.InDB(e => e.Ticks));
        }

        public Dictionary<string, int> GetSymbolIds(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                 () => Symbol.GetSymbolIds(type));
        }

        public Dictionary<string, Tuple<int,string>> GetSemiSymbolIdsAndNames(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                 () => SemiSymbol.GetSemiSymbolIdsAndNames(type));
        }
        #endregion

        #region IDynamicQueryServer
        public virtual QueryDescription GetQueryDescription(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => DynamicQueryManager.Current.QueryDescription(queryName));
        }

        public virtual ResultTable ExecuteQuery(QueryRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(), request.QueryName.ToString(),
                () => DynamicQueryManager.Current.ExecuteQuery(request));
        }

        public ResultTable ExecuteQueryGroup(QueryGroupRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(), request.QueryName.ToString(),
                () => DynamicQueryManager.Current.ExecuteGroupQuery(request));
        }

        public virtual int ExecuteQueryCount(QueryCountRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(), request.QueryName.ToString(),
                () => DynamicQueryManager.Current.ExecuteQueryCount(request));
        }

        public virtual Lite<IdentifiableEntity> ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(), request.QueryName.ToString(),
                () => DynamicQueryManager.Current.ExecuteUniqueEntity(request));
        }

        public virtual List<object> GetQueryNames()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => DynamicQueryManager.Current.GetQueryNames());
        }

        public virtual List<QueryToken> ExternalQueryToken(QueryToken parent)
        {
            return Return(MethodInfo.GetCurrentMethod(),
               () => DynamicQueryManager.Current.GetExtensions(parent).ToList());
        }

        public virtual object[] BatchExecute(BaseQueryRequest[] requests)
        {
            return Return(MethodInfo.GetCurrentMethod(), requests.ToString("; "),
                () => DynamicQueryManager.Current.BatchExecute(requests));
        }
        #endregion

        #region IOperationServer Members
        public Dictionary<OperationSymbol, string> GetCanExecuteAll(IdentifiableEntity entity)
        {
            return Return(MethodInfo.GetCurrentMethod(), entity.GetType().Name,
                () => OperationLogic.ServiceCanExecute(entity));
        }

        public Dictionary<OperationSymbol, string> GetCanExecuteLiteAll(Lite<IdentifiableEntity> lite)
        {
            return Return(MethodInfo.GetCurrentMethod(), lite.EntityType.Name,
                () => OperationLogic.ServiceCanExecute(Database.Retrieve(lite)));
        }

        public string GetCanExecute(IdentifiableEntity entity, OperationSymbol operationSymbol)
        {
            return Return(MethodInfo.GetCurrentMethod(), entity.GetType().Name + " " + operationSymbol,
                () => OperationLogic.ServiceCanExecute(entity, operationSymbol));
        }

        public string GetCanExecuteLite(Lite<IdentifiableEntity> lite, OperationSymbol operationSymbol)
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

        public HashSet<Type> GetSaveProtectedTypes()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => OperationLogic.GetSaveProtectedTypes());
        }

        public IdentifiableEntity ExecuteOperation(IIdentifiable entity, OperationSymbol operationSymbol, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationSymbol.ToString(),
                () => OperationLogic.ServiceExecute(entity, operationSymbol, args));
        }

        public IdentifiableEntity ExecuteOperationLite(Lite<IIdentifiable> lite, OperationSymbol operationSymbol, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationSymbol.ToString(),
                () => (IdentifiableEntity)OperationLogic.ServiceExecuteLite(lite, operationSymbol, args));
        }

        public void Delete(Lite<IIdentifiable> lite, OperationSymbol operationSymbol, params object[] args)
        {
            Execute(MethodInfo.GetCurrentMethod(), operationSymbol.ToString(),
                () => OperationLogic.ServiceDelete(lite, operationSymbol, args));
        }

        public IdentifiableEntity Construct(Type type, OperationSymbol operationSymbol, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationSymbol.ToString(),
                () => OperationLogic.ServiceConstruct(type, operationSymbol, args));
        }

        public IdentifiableEntity ConstructFrom(IIdentifiable entity, OperationSymbol operationSymbol, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationSymbol.ToString(),
                () => OperationLogic.ServiceConstructFrom(entity, operationSymbol, args));
        }

        public IdentifiableEntity ConstructFromLite(Lite<IIdentifiable> lite, OperationSymbol operationSymbol, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationSymbol.ToString(),
                () => OperationLogic.ServiceConstructFromLite(lite, operationSymbol, args));
        }

        public IdentifiableEntity ConstructFromMany(IEnumerable<Lite<IIdentifiable>> lites, Type type, OperationSymbol operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationKey.ToString(),
                () => OperationLogic.ServiceConstructFromMany(lites, type, operationKey, args));
        }

        public Dictionary<OperationSymbol, string> GetContextualCanExecute(Lite<IIdentifiable>[] lite, List<OperationSymbol> operatonSymbols)
        {
            return Return(MethodInfo.GetCurrentMethod(), null,
                () => OperationLogic.GetContextualCanExecute(lite, operatonSymbols));
        }

        #endregion


      
    }
}
