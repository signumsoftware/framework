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

namespace Signum.Services
{
    public abstract class ServerExtensions : ServerBasic, ILoginServer, IOperationServer,
        IQueryAuthServer, IPropertyAuthServer, ITypeAuthServer, IFacadeMethodAuthServer, IPermissionAuthServer, IOperationAuthServer 
    {
        protected UserDN currentUser;

        protected override void Execute(MethodBase mi, Action action)
        {
            try
            {
                Thread.CurrentPrincipal = currentUser;

                FacadeMethodAuthLogic.AuthorizeAccess((MethodInfo)mi);

                action();
            }
            catch (Exception e)
            {
                throw new FaultException(e.Message);
            }
            finally
            {
                Thread.CurrentPrincipal = null;
            }
        }

        #region ILoginServer Members

        public void Login(string username, string passwordHash)
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
            return Return(MethodInfo.GetCurrentMethod(),
                () => OperationLogic.GetEntityOperationInfos(entity));
        }

        public List<OperationInfo> GetQueryOperationInfos(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => OperationLogic.GetQueryOperationInfos(entityType));
        }

        public List<OperationInfo> GetConstructorOperationInfos(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => OperationLogic.GetConstructorOperationInfos(entityType));
        }

        public IdentifiableEntity ExecuteOperation(IdentifiableEntity entity, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(),
               () => OperationLogic.Execute(entity, operationKey, args));
        }

        public IdentifiableEntity ExecuteOperationLazy(Lazy lazy, Enum type, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => OperationLogic.ExecuteLazy(lazy, type, args));
        }

        public IdentifiableEntity Construct(Type type, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => OperationLogic.Construct(type, operationKey, args));
        }

        public IdentifiableEntity ConstructFrom(IIdentifiable entity, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => OperationLogic.ConstructFrom(entity, operationKey, args));
        }

        public IdentifiableEntity ConstructFromLazy(Lazy lazy, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => OperationLogic.ConstructFrom(lazy, operationKey, args));
        }

        public IdentifiableEntity ConstructFromMany(List<Lazy> lazies, Type type, Enum operationKey, params object[] args)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => OperationLogic.ConstructFromMany(lazies, type, operationKey, args));
        }
        #endregion

        #region IPropertyAuthServer Members

        public List<AccessRule> GetPropertyAccessRules(Lazy<RoleDN> role, TypeDN typeDN)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => PropertyAuthLogic.GetAccessRule(role, typeDN));
        }

        public void SetPropertyAccessRules(List<AccessRule> rules, Lazy<RoleDN> role, TypeDN typeDN)
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

        public List<TypeAccessRule> GetTypesAccessRules(Lazy<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => TypeAuthLogic.GetAccessRule(role));
        }

        public void SetTypesAccessRules(List<TypeAccessRule> rules, Lazy<RoleDN> role)
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

        public List<AllowedRule> GetFacadeMethodAllowedRules(Lazy<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => FacadeMethodAuthLogic.GetAllowedRule(role));
        }

        public void SetFacadeMethodAllowedRules(List<AllowedRule> rules, Lazy<RoleDN> role)
        {
            Execute(MethodInfo.GetCurrentMethod(),
              () => FacadeMethodAuthLogic.SetAllowedRule(rules, role));
        }

        #endregion

        #region IQueryAuthServer Members

        public List<AllowedRule> GetQueryAllowedRules(Lazy<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => QueryAuthLogic.GetAllowedRule(role));
        }

        public void SetQueryAllowedRules(List<AllowedRule> rules, Lazy<RoleDN> role)
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

        public List<AllowedRule> GetPermissionAllowedRules(Lazy<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => PermissionAuthLogic.GetAllowedRule(role));
        }

        public void SetPermissionAllowedRules(List<AllowedRule> rules, Lazy<RoleDN> role)
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

        public List<AllowedRule> GetOperationAllowedRules(Lazy<RoleDN> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => OperationAuthLogic.GetAllowedRule(role));
        }


        public void SetOperationAllowedRules(List<AllowedRule> rules, Lazy<RoleDN> role)
        {
            Execute(MethodInfo.GetCurrentMethod(),
               () => OperationAuthLogic.SetAllowedRule(rules, role));
        }

        #endregion

    }
}
