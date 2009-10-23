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
        List<TypeAccessRule> GetTypesAccessRules(Lazy<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetTypesAccessRules(List<TypeAccessRule> rules, Lazy<RoleDN> role);

        [OperationContract, NetDataContract]
        Dictionary<Type, TypeAccess> AuthorizedTypes(); 
    }

    [ServiceContract]
    public interface IFacadeMethodAuthServer
    {
        [OperationContract, NetDataContract]
        List<AllowedRule> GetFacadeMethodAllowedRules(Lazy<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetFacadeMethodAllowedRules(List<AllowedRule> rules, Lazy<RoleDN> role); 
    }

    [ServiceContract]
    public interface IPermissionAuthServer
    {
        [OperationContract, NetDataContract]
        List<AllowedRule> GetPermissionAllowedRules(Lazy<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetPermissionAllowedRules(List<AllowedRule> rules, Lazy<RoleDN> role);

        [OperationContract, NetDataContract]
        bool IsAuthorizedFor(Enum permissionKey);
    }

    [ServiceContract]
    public interface IPropertyAuthServer
    {
        [OperationContract, NetDataContract]
        List<AccessRule> GetPropertyAccessRules(Lazy<RoleDN> role, TypeDN typeDN);

        [OperationContract, NetDataContract]
        void SetPropertyAccessRules(List<AccessRule> rules, Lazy<RoleDN> role, TypeDN typeDN);

        [OperationContract, NetDataContract]
        Dictionary<Type, Dictionary<string, Access>> AuthorizedProperties(); 
    }

    [ServiceContract]
    public interface IQueryAuthServer
    {
        [OperationContract, NetDataContract]
        List<AllowedRule> GetQueryAllowedRules(Lazy<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetQueryAllowedRules(List<AllowedRule> rules, Lazy<RoleDN> role);

        [OperationContract, NetDataContract]
        HashSet<object> AuthorizedQueries();
    }

    [ServiceContract]
    public interface IOperationAuthServer
    {
        [OperationContract, NetDataContract]
        List<AllowedRule> GetOperationAllowedRules(Lazy<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetOperationAllowedRules(List<AllowedRule> rules, Lazy<RoleDN> role);
    }

    [ServiceContract]
    public interface IProcessAuthServer
    {
        [OperationContract, NetDataContract]
        List<AllowedRule> GetOperationAllowedRules(Lazy<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetOperationAllowedRules(List<AllowedRule> rules, Lazy<RoleDN> role);
    }
}
