using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities.Authorization;
using Signum.Entities;

namespace Signum.Services
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface ILoginServer
    {
        [OperationContract, NetDataContract]
        void Login(string username, string passwordHash);

        [OperationContract(IsTerminating = true), NetDataContract]
        void Logout();

        [OperationContract, NetDataContract]
        UserDN GetCurrentUser();
    }

    [ServiceContract]
    public interface ITypeAuthServer
    {
        [OperationContract, NetDataContract]
        List<TypeAccessRule> GetTypesAccessRules(Lite<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetTypesAccessRules(List<TypeAccessRule> rules, Lite<RoleDN> role);

        [OperationContract, NetDataContract]
        Dictionary<Type, TypeAccess> AuthorizedTypes(); 
    }

    [ServiceContract]
    public interface IFacadeMethodAuthServer
    {
        [OperationContract, NetDataContract]
        List<AllowedRule> GetFacadeMethodAllowedRules(Lite<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetFacadeMethodAllowedRules(List<AllowedRule> rules, Lite<RoleDN> role); 
    }

    [ServiceContract]
    public interface IPermissionAuthServer
    {
        [OperationContract, NetDataContract]
        List<AllowedRule> GetPermissionAllowedRules(Lite<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetPermissionAllowedRules(List<AllowedRule> rules, Lite<RoleDN> role);

        [OperationContract, NetDataContract]
        Dictionary<Enum, bool> PermissionRules();
    }

    [ServiceContract]
    public interface IPropertyAuthServer
    {
        [OperationContract, NetDataContract]
        List<AccessRule> GetPropertyAccessRules(Lite<RoleDN> role, TypeDN typeDN);

        [OperationContract, NetDataContract]
        void SetPropertyAccessRules(List<AccessRule> rules, Lite<RoleDN> role, TypeDN typeDN);

        [OperationContract, NetDataContract]
        Dictionary<Type, Dictionary<string, Access>> AuthorizedProperties(); 
    }

    [ServiceContract]
    public interface IQueryAuthServer
    {
        [OperationContract, NetDataContract]
        List<AllowedRule> GetQueryAllowedRules(Lite<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetQueryAllowedRules(List<AllowedRule> rules, Lite<RoleDN> role);

        [OperationContract, NetDataContract]
        HashSet<object> AuthorizedQueries();
    }

    [ServiceContract]
    public interface IOperationAuthServer
    {
        [OperationContract, NetDataContract]
        List<AllowedRule> GetOperationAllowedRules(Lite<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetOperationAllowedRules(List<AllowedRule> rules, Lite<RoleDN> role);
    }

    [ServiceContract]
    public interface IEntityGroupAuthServer
    {
        [OperationContract, NetDataContract]
        List<EntityGroupRule> GetEntityGroupAllowedRules(Lite<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetEntityGroupAllowedRules(List<EntityGroupRule> rules, Lite<RoleDN> role);

        //   #region IEntityGroupAuthServer Members

        //   public List<EntityGroupRule> GetEntityGroupAllowedRules(Lite<RoleDN> role)
        //   {
        //       return Return(MethodInfo.GetCurrentMethod(),
        //        () => EntityGroupAuthLogic.GetEntityGroupRules(role));
        //   }

        //   public void SetEntityGroupAllowedRules(List<EntityGroupRule> rules, Lite<RoleDN> role)
        //   {
        //       Execute(MethodInfo.GetCurrentMethod(),
        //          () => EntityGroupAuthLogic.SetEntityGroupRules(rules, role));
        //   }

        //   #endregion
    }


}
