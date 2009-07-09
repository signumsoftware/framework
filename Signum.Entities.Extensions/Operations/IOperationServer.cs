using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities.Operations;
using Signum.Entities;

namespace Signum.Services
{
    [ServiceContract]
    public interface IOperationServer
    {
        [OperationContract, NetDataContract]
        List<OperationInfo> GetEntityOperationInfos(Lazy lazy);

        [OperationContract, NetDataContract]
        List<OperationInfo> GetQueryOperationInfos(Type entityType);

        [OperationContract, NetDataContract]
        List<OperationInfo> GetConstructorOperationInfos(Type entityType);


        [OperationContract, NetDataContract]
        IdentifiableEntity ExecuteOperation(IdentifiableEntity entity, Enum operationKey, params object[] args);

        [OperationContract, NetDataContract]
        IdentifiableEntity ExecuteOperationLazy(Lazy lazy, Enum operationKey, params object[] args);


        [OperationContract, NetDataContract]
        IdentifiableEntity Construct(Type type, Enum operationKey, params object[] args);


        [OperationContract, NetDataContract]
        IdentifiableEntity ConstructFrom(IIdentifiable entity, Type type, Enum operationKey, params object[] args);

        [OperationContract, NetDataContract]
        IdentifiableEntity ConstructFrom(Lazy lazy, Type type, Enum operationKey, params object[] args);


        [OperationContract, NetDataContract]
        IdentifiableEntity ConstructFromMany(List<Lazy> lazies, Type type, Enum operationKey, params object[] args);
    }
}
