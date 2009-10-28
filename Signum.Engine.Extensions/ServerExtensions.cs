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

namespace Signum.Services
{
    public abstract class ServerExtensions : ServerBasic, ILoginServer, IOperationServer,
        IQueryAuthServer, IPropertyAuthServer, ITypeAuthServer, IFacadeMethodAuthServer, IPermissionAuthServer, IOperationAuthServer, IEntityGroupAuthServer 
    {
        protected UserDN currentUser;

        protected override void Execute(MethodBase mi, string description, Action action)
        {
            try
            {
                using (AuthLogic.User(currentUser))
                {
                    FacadeMethodAuthLogic.AuthorizeAccess((MethodInfo)mi);

                    action();
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

        public IdentifiableEntity ExecuteOperation(IdentifiableEntity entity, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), "ExecuteOperation {0}".Formato(operationKey),
               () => OperationLogic.ServiceExecute(entity, operationKey, args));
        }

        public IdentifiableEntity ExecuteOperationLite(Lite lazy, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), "ExecuteOperationLite {0}".Formato(operationKey),
              () => OperationLogic.ServiceExecuteLite(lazy, operationKey, args));
        }

        public IdentifiableEntity Construct(Type type, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), "Construct {0}".Formato(operationKey),
              () => OperationLogic.ServiceConstruct(type, operationKey, args));
        }

        public IdentifiableEntity ConstructFrom(IIdentifiable entity, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), "ConstructFrom {0}".Formato(operationKey),
              () => OperationLogic.ServiceConstructFrom(entity, operationKey, args));
        }

        public IdentifiableEntity ConstructFromLite(Lite lazy, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), "ConstructFromLite {0}".Formato(operationKey),
              () => OperationLogic.ServiceConstructFromLite(lazy, operationKey, args));
        }

        public IdentifiableEntity ConstructFromMany(List<Lite> lazies, Type type, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(), "ConstructFromMany {0}".Formato(operationKey),
              () => OperationLogic.ServiceConstructFromMany(lazies, type, operationKey, args));
        }
        #endregion

        #region IPropertyAuthServer Members

        public List<AccessRule> GetPropertyAccessRules(Lite<RoleDN> role, TypeDN typeDN)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => PropertyAuthLogic.GetAccessRule(role, typeDN));
        }

        public void SetPropertyAccessRules(List<AccessRule> rules, Lite<RoleDN> role, TypeDN typeDN)
        {
            Execute(MethodInfo.GetCurrentMethod(),
             () => PropertyAuthLogic.SetAccessRule(rules, role, typeDN));
        }

        public Dictionary<Type, Dictionary<string, Access>> AuthorizedProperties()
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => PropertyAuthLogic.AuthorizedProperties());
        }

        #endregion

        #region ITypeAuthServer Members

        public List<TypeAccessRule> GetTypesAccessRules(Lite<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => TypeAuthLogic.GetAccessRule(role));
        }

        public void SetTypesAccessRules(List<TypeAccessRule> rules, Lite<RoleDN> role)
        {
            Execute(MethodInfo.GetCurrentMethod(),
              () => TypeAuthLogic.SetAccessRule(rules, role));
        }

        public Dictionary<Type, TypeAccess> AuthorizedTypes()
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => TypeAuthLogic.AuthorizedTypes());
        }

        #endregion

        #region IFacadeMethodAuthServer Members

        public List<AllowedRule> GetFacadeMethodAllowedRules(Lite<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => FacadeMethodAuthLogic.GetAllowedRule(role));
        }

        public void SetFacadeMethodAllowedRules(List<AllowedRule> rules, Lite<RoleDN> role)
        {
            Execute(MethodInfo.GetCurrentMethod(),
              () => FacadeMethodAuthLogic.SetAllowedRule(rules, role));
        }

        #endregion

        #region IQueryAuthServer Members

        public List<AllowedRule> GetQueryAllowedRules(Lite<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => QueryAuthLogic.GetAllowedRule(role));
        }

        public void SetQueryAllowedRules(List<AllowedRule> rules, Lite<RoleDN> role)
        {
            Execute(MethodInfo.GetCurrentMethod(),
               () => QueryAuthLogic.SetAllowedRule(rules, role));
        }

        public HashSet<object> AuthorizedQueries()
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => QueryAuthLogic.AuthorizedQueryNames(GetQueryManager()));
        }

        #endregion

        #region IPermissionAuthServer Members

        public List<AllowedRule> GetPermissionAllowedRules(Lite<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => PermissionAuthLogic.GetAllowedRule(role));
        }

        public void SetPermissionAllowedRules(List<AllowedRule> rules, Lite<RoleDN> role)
        {
            Execute(MethodInfo.GetCurrentMethod(),
            () => PermissionAuthLogic.SetAllowedRule(rules, role));
        }

        public bool IsAuthorizedFor(Enum permissionKey)
        {
            return Return(MethodInfo.GetCurrentMethod(),
           () => PermissionAuthLogic.IsAuthorizedFor(permissionKey));
        }

        #endregion

        #region IOperationAuthServer Members

        public List<AllowedRule> GetOperationAllowedRules(Lite<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => OperationAuthLogic.GetAllowedRule(role));
        }


        public void SetOperationAllowedRules(List<AllowedRule> rules, Lite<RoleDN> role)
        {
            Execute(MethodInfo.GetCurrentMethod(),
               () => OperationAuthLogic.SetAllowedRule(rules, role));
        }

        #endregion

        #region IEntityGroupAuthServer Members

        public List<EntityGroupRule> GetEntityGroupAllowedRules(Lite<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => EntityGroupAuthLogic.GetEntityGroupRules(role));
        }

        public void SetEntityGroupAllowedRules(List<EntityGroupRule> rules, Lite<RoleDN> role)
        {
            Execute(MethodInfo.GetCurrentMethod(),
               () => EntityGroupAuthLogic.SetEntityGroupRules(rules, role));
        }

        #endregion
    }
}
