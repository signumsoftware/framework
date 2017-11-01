using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Entities.Basics;

namespace Signum.Services
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface ILoginServer
    {
        [OperationContract, NetDataContract]
        void Login(string username, byte[] passwordHash);

        [OperationContract, NetDataContract]
        void LoginChagePassword(string username, byte[] passwordHash, byte[] newPasswordHash);

        [OperationContract, NetDataContract]
        void ChagePassword(Lite<UserEntity> user, byte[] passwordHash, byte[] newPasswordHash);

        [OperationContract, NetDataContract]
        UserEntity GetCurrentUser();

        [OperationContract, NetDataContract]
        string PasswordNearExpired();

        [OperationContract, NetDataContract]
        byte[] DownloadAuthRules();
    }

    [ServiceContract]
    public interface ITypeAuthServer
    {
        [OperationContract, NetDataContract]
        TypeRulePack GetTypesRules(Lite<RoleEntity> role);

        [OperationContract, NetDataContract]
        void SetTypesRules(TypeRulePack rules);

        [OperationContract, NetDataContract]
        DefaultDictionary<Type, TypeAllowedAndConditions> AuthorizedTypes();

        [OperationContract, NetDataContract]
        bool IsAllowedForInUserInterface(Lite<IEntity> lite, TypeAllowedBasic allowed);
    }

    [ServiceContract]
    public interface IPermissionAuthServer
    {
        [OperationContract, NetDataContract]
        PermissionRulePack GetPermissionRules(Lite<RoleEntity> role);

        [OperationContract, NetDataContract]
        void SetPermissionRules(PermissionRulePack rules);

        [OperationContract, NetDataContract]
        DefaultDictionary<PermissionSymbol, bool> PermissionRules();
    }

    [ServiceContract]
    public interface IPropertyAuthServer
    {
        [OperationContract, NetDataContract]
        PropertyRulePack GetPropertyRules(Lite<RoleEntity> role, TypeEntity typeEntity);

        [OperationContract, NetDataContract]
        void SetPropertyRules(PropertyRulePack rules);

        [OperationContract, NetDataContract]
        Dictionary<PropertyRoute, PropertyAllowed> OverridenProperties();
    }

    [ServiceContract]
    public interface IQueryAuthServer
    {
        [OperationContract, NetDataContract]
        QueryRulePack GetQueryRules(Lite<RoleEntity> role, TypeEntity typeEntity);

        [OperationContract, NetDataContract]
        void SetQueryRules(QueryRulePack rules);

        [OperationContract, NetDataContract]
        HashSet<object> AllowedQueries();
    }

    [ServiceContract]
    public interface IOperationAuthServer
    {
        [OperationContract, NetDataContract]
        OperationRulePack GetOperationRules(Lite<RoleEntity> role, TypeEntity typeEntity);

        [OperationContract, NetDataContract]
        void SetOperationRules(OperationRulePack rules);

        [OperationContract, NetDataContract]
        Dictionary<(OperationSymbol operation, Type type), OperationAllowed> AllowedOperations();
    }
}
