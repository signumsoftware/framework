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

        protected virtual T Return<T>(MethodBase mi, string description, Func<T> function, bool checkLogin = true)
        {
            try
            {
                using (ScopeSessionFactory.OverrideSession(session))
                {
                    return function();
                }
            }
            catch (Exception e)
            {
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

        protected void Execute(MethodBase mi, string description, Action action, bool checkLogin = true)
        {
            Return(mi, description, () => { action(); return true; }, checkLogin);
        }

        #region IBaseServer
        public virtual IdentifiableEntity Retrieve(Type type, int id)
        {
            return Return(MethodInfo.GetCurrentMethod(), type.Name,
                () =>Database.Retrieve(type, id));
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
                () => AutoCompleteUtils.FindAllLite(implementations));
        }

        public virtual List<Lite<IdentifiableEntity>> FindLiteLike(Implementations implementations, string subString, int count)
        {
            return Return(MethodInfo.GetCurrentMethod(), implementations.ToString(),
                () => AutoCompleteUtils.FindLiteLike(implementations, subString, count));
        }

        public virtual Dictionary<PropertyRoute, Implementations> FindAllImplementations(Type root)
        {
            return Return(MethodInfo.GetCurrentMethod(), root.Name,
                () => Schema.Current.FindAllImplementations(root));
        }

        public virtual bool Exists(Type type, int id)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                  () => Database.Exists(type, id));
        }

        public virtual Dictionary<Type, TypeDN> ServerTypes()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => Schema.Current.TypeToDN);
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
            return Return(MethodInfo.GetCurrentMethod(),
             () => DynamicQueryManager.Current.BatchExecute(requests));
        }
        #endregion

        #region IOperationServer Members
        public Dictionary<Enum, string> GetCanExecute(IdentifiableEntity entity)
        {
            return Return(MethodInfo.GetCurrentMethod(), entity.GetType().Name,
                () => OperationLogic.ServiceCanExecute(entity));
        }

        public Dictionary<Enum, string> GetCanExecuteLite(Lite<IdentifiableEntity> lite)
        {
            return Return(MethodInfo.GetCurrentMethod(), lite.EntityType.Name,
                () => OperationLogic.ServiceCanExecute(Database.Retrieve(lite)));
        }

        public List<OperationInfo> GetOperationInfos(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => OperationLogic.ServiceGetOperationInfos(entityType));
        }

        public HashSet<Type> GetSaveProtectedTypes()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => OperationLogic.GetSaveProtectedTypes());
        }

        public IdentifiableEntity ExecuteOperation(IIdentifiable entity, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationKey.ToString(),
                () => (IdentifiableEntity)OperationLogic.Execute(entity, operationKey, args));
        }

        public IdentifiableEntity ExecuteOperationLite(Lite<IIdentifiable> lite, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationKey.ToString(),
                () => (IdentifiableEntity)OperationLogic.ExecuteLite(lite, operationKey, args));
        }

        public void Delete(Lite<IIdentifiable> lite, Enum operationKey, params object[] args)
        {
            Execute(MethodInfo.GetCurrentMethod(), operationKey.ToString(),
                () => OperationLogic.Delete(lite, operationKey, args));
        }

        public IdentifiableEntity Construct(Type type, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationKey.ToString(),
                () => OperationLogic.Construct(type, operationKey, args));
        }

        public IdentifiableEntity ConstructFrom(IIdentifiable entity, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationKey.ToString(),
                () => OperationLogic.ConstructFrom<IdentifiableEntity>(entity, operationKey, args));
        }

        public IdentifiableEntity ConstructFromLite(Lite<IIdentifiable> lite, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationKey.ToString(),
                () => OperationLogic.ConstructFromLite<IdentifiableEntity>(lite, operationKey, args));
        }

        public IdentifiableEntity ConstructFromMany(IEnumerable<Lite<IIdentifiable>> lites, Type type, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), operationKey.ToString(),
                () => OperationLogic.ServiceConstructFromMany(lites, type, operationKey, args));
        }

        public Dictionary<Enum, string> GetContextualCanExecute(Lite<IIdentifiable>[] lite, List<Enum> cleanKeys)
        {
            return Return(MethodInfo.GetCurrentMethod(), null,
                () => OperationLogic.GetContextualCanExecute(lite, cleanKeys));
        }

        #endregion
    }
}
