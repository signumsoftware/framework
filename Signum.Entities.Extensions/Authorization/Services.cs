using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Utilities.DataStructures;

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
        TypeRulePack GetTypesRules(Lite<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetTypesRules(TypeRulePack rules);

        [OperationContract, NetDataContract]
        Dictionary<Type, TypeAllowedBasic> AuthorizedTypes(); 
    }

    [ServiceContract]
    public interface IFacadeMethodAuthServer
    {
        [OperationContract, NetDataContract]
        FacadeMethodRulePack GetFacadeMethodRules(Lite<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetFacadeMethodRules(FacadeMethodRulePack rules); 
    }

    [ServiceContract]
    public interface IPermissionAuthServer
    {
        [OperationContract, NetDataContract]
        PermissionRulePack GetPermissionRules(Lite<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetPermissionRules(PermissionRulePack rules);

        [OperationContract, NetDataContract]
        Dictionary<Enum, bool> PermissionRules();
    }

    [ServiceContract]
    public interface IPropertyAuthServer
    {
        [OperationContract, NetDataContract]
        PropertyRulePack GetPropertyRules(Lite<RoleDN> role, TypeDN typeDN);

        [OperationContract, NetDataContract]
        void SetPropertyRules(PropertyRulePack rules);

        [OperationContract, NetDataContract]
        Dictionary<PropertyRoute, PropertyAllowed> AuthorizedProperties(); 
    }

    [ServiceContract]
    public interface IQueryAuthServer
    {
        [OperationContract, NetDataContract]
        QueryRulePack GetQueryRules(Lite<RoleDN> role, TypeDN typeDN);

        [OperationContract, NetDataContract]
        void SetQueryRules(QueryRulePack rules);

        [OperationContract, NetDataContract]
        HashSet<object> AuthorizedQueries();
    }

    [ServiceContract]
    public interface IOperationAuthServer
    {
        [OperationContract, NetDataContract]
        OperationRulePack GetOperationRules(Lite<RoleDN> role, TypeDN typeDN);

        [OperationContract, NetDataContract]
        void SetOperationRules(OperationRulePack rules);
    }

    [ServiceContract]
    public interface IEntityGroupAuthServer
    {
        [OperationContract, NetDataContract]
        EntityGroupRulePack GetEntityGroupAllowedRules(Lite<RoleDN> role);

        [OperationContract, NetDataContract]
        void SetEntityGroupAllowedRules(EntityGroupRulePack rules);

        [OperationContract, NetDataContract]
        Dictionary<Type, MinMax<TypeAllowedBasic>> GetEntityGroupTypesAllowed();

        [OperationContract, NetDataContract]
        bool IsAllowedFor(Lite lite, TypeAllowedBasic allowed);
    }
}
