using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities;
using Signum.Entities.Basics;
using System.Reflection;
using System.Threading;
using Signum.Entities.Authorization;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Engine.Authorization;
using Signum.Entities.Operations;
using Signum.Engine.Operations;
using Signum.Utilities;
using Signum.Engine.Basics;
using Signum.Engine.Extensions.Chart;
using Signum.Entities.Chart;


namespace Signum.Services
{
    public abstract class ServerExtensions : ServerBasic, ILoginServer, IOperationServer, IQueryServer, IChartServer,
        IQueryAuthServer, IPropertyAuthServer, ITypeAuthServer, IFacadeMethodAuthServer, IPermissionAuthServer, IOperationAuthServer
    {
        protected UserDN currentUser;

        protected override T Return<T>(MethodBase mi, string description, Func<T> function)
        {
            try
            {
                using (AuthLogic.User(currentUser))
                {
                    FacadeMethodAuthLogic.AuthorizeAccess((MethodInfo)mi);

                    return function();
                }
            }
            catch (Exception e)
            {
                throw new FaultException(e.Message);
            }
        }

        #region ILoginServer Members

        public virtual void Login(string username, string passwordHash)
        {
            try
            {
                currentUser = AuthLogic.Login(username, passwordHash);
            }
            catch (Exception e)
            {
                throw new FaultException(e.Message);
            }
        }

        public void Logout()
        {
            this.currentUser = null;
        }

        public UserDN GetCurrentUser()
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => currentUser);
        }


        #endregion

        #region IOperationServer Members
        public List<OperationInfo> GetEntityOperationInfos(IdentifiableEntity entity)
        {
            return Return(MethodInfo.GetCurrentMethod(), "GetEntityOperationInfos {0}".Formato(entity.GetType()),
                () => OperationLogic.ServiceGetEntityOperationInfos(entity));
        }

        public List<OperationInfo> GetQueryOperationInfos(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => OperationLogic.ServiceGetQueryOperationInfos(entityType));
        }

        public List<OperationInfo> GetConstructorOperationInfos(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => OperationLogic.ServiceGetConstructorOperationInfos(entityType));
        }

        public IIdentifiable ExecuteOperation(IIdentifiable entity, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), "ExecuteOperation {0}".Formato(operationKey),
               () => OperationLogic.ServiceExecute(entity, operationKey, args));
        }

        public IIdentifiable ExecuteOperationLite(Lite lite, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), "ExecuteOperationLite {0}".Formato(operationKey),
              () => OperationLogic.ServiceExecuteLite(lite, operationKey, args));
        }

        public IIdentifiable Delete(Lite lite, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), "Delete {0}".Formato(operationKey),
              () => OperationLogic.ServiceDelete(lite, operationKey, args));
        }

        public IIdentifiable Construct(Type type, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), "Construct {0}".Formato(operationKey),
              () => OperationLogic.ServiceConstruct(type, operationKey, args));
        }

        public IIdentifiable ConstructFrom(IIdentifiable entity, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), "ConstructFrom {0}".Formato(operationKey),
              () => OperationLogic.ServiceConstructFrom(entity, operationKey, args));
        }

        public IIdentifiable ConstructFromLite(Lite lite, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), "ConstructFromLite {0}".Formato(operationKey),
              () => OperationLogic.ServiceConstructFromLite(lite, operationKey, args));
        }

        public IIdentifiable ConstructFromMany(List<Lite> lites, Type type, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), "ConstructFromMany {0}".Formato(operationKey),
              () => OperationLogic.ServiceConstructFromMany(lites, type, operationKey, args));
        }
        #endregion

        #region IQueryServer Members

        public QueryDN RetrieveOrGenerateQuery(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => QueryLogic.RetrieveOrGenerateQuery(queryName));
        }

        #endregion

        #region IPropertyAuthServer Members

        public PropertyRulePack GetPropertyRules(Lite<RoleDN> role, TypeDN typeDN)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => PropertyAuthLogic.GetPropertyRules(role, typeDN));
        }

        public void SetPropertyRules(PropertyRulePack rules)
        {
            Execute(MethodInfo.GetCurrentMethod(),
             () => PropertyAuthLogic.SetPropertyRules(rules));
        }

        public Dictionary<PropertyRoute, PropertyAllowed> AuthorizedProperties()
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => PropertyAuthLogic.AuthorizedProperties());
        }

        #endregion

        #region ITypeAuthServer Members

        public TypeRulePack GetTypesRules(Lite<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => TypeAuthLogic.GetTypeRules(role));
        }

        public void SetTypesRules(TypeRulePack rules)
        {
            Execute(MethodInfo.GetCurrentMethod(),
              () => TypeAuthLogic.SetTypeRules(rules));
        }

        public Dictionary<Type, TypeAllowed> AuthorizedTypes()
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => TypeAuthLogic.AuthorizedTypes());
        }

        #endregion

        #region IFacadeMethodAuthServer Members

        public FacadeMethodRulePack GetFacadeMethodRules(Lite<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => FacadeMethodAuthLogic.GetFacadeMethodRules(role));
        }

        public void SetFacadeMethodRules(FacadeMethodRulePack rules)
        {
            Execute(MethodInfo.GetCurrentMethod(),
              () => FacadeMethodAuthLogic.SetFacadeMethodRules(rules));
        }

        #endregion

        #region IQueryAuthServer Members

        public QueryRulePack GetQueryRules(Lite<RoleDN> role, TypeDN typeDN)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => QueryAuthLogic.GetQueryRules(role, typeDN));
        }

        public void SetQueryRules(QueryRulePack rules)
        {
            Execute(MethodInfo.GetCurrentMethod(),
               () => QueryAuthLogic.SetQueryRules(rules));
        }

        public HashSet<object> AuthorizedQueries()
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => QueryAuthLogic.AuthorizedQueryNames());
        }

        #endregion

        #region IPermissionAuthServer Members

        public PermissionRulePack GetPermissionRules(Lite<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => PermissionAuthLogic.GetPermissionRules(role));
        }

        public void SetPermissionRules(PermissionRulePack rules)
        {
            Execute(MethodInfo.GetCurrentMethod(),
            () => PermissionAuthLogic.SetPermissionRules(rules));
        }

        public Dictionary<Enum, bool> PermissionRules()
        {
            return Return(MethodInfo.GetCurrentMethod(),
           () => PermissionAuthLogic.ServicePermissionRules());
        }

        #endregion

        #region IOperationAuthServer Members
        public OperationRulePack GetOperationRules(Lite<RoleDN> role, TypeDN typeDN)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => OperationAuthLogic.GetOperationRules(role, typeDN));
        }

        public void SetOperationRules(OperationRulePack rules)
        {
            Execute(MethodInfo.GetCurrentMethod(),
               () => OperationAuthLogic.SetOperationRules(rules));
        }
        #endregion

        #region IChartServer
        public ResultTable ExecuteChart(ChartRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(),
               () => ChartLogic.ExecuteChart(request));
        }

        public List<Lite<UserChartDN>> GetUserCharts(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => ChartLogic.GetUserCharts(queryName));
        }

        public void RemoveUserChart(Lite<UserChartDN> lite)
        {
            Execute(MethodInfo.GetCurrentMethod(),
              () => ChartLogic.RemoveUserChart(lite));
        }

        #endregion
    } 
}
