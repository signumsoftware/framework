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
        List<OperationInfo> GetEntityOperationInfos(IdentifiableEntity lite);

        [OperationContract, NetDataContract]
        List<OperationInfo> GetOperationInfos(Type entityType);

        [OperationContract, NetDataContract]
        bool GetSaveProtected(Type entityType);

        [OperationContract, NetDataContract]
        IIdentifiable ExecuteOperation(IIdentifiable entity, Enum operationKey, params object[] args);

        [OperationContract, NetDataContract]
        IIdentifiable ExecuteOperationLite(Lite lite, Enum operationKey, params object[] args);


        [OperationContract, NetDataContract]
        IIdentifiable Delete(Lite lite, Enum operationKey, params object[] args);


        [OperationContract, NetDataContract]
        IIdentifiable Construct(Type type, Enum operationKey, params object[] args);


        [OperationContract, NetDataContract]
        IIdentifiable ConstructFrom(IIdentifiable entity, Enum operationKey, params object[] args);

        [OperationContract, NetDataContract]
        IIdentifiable ConstructFromLite(Lite lite, Enum operationKey, params object[] args);


        [OperationContract, NetDataContract]
        IIdentifiable ConstructFromMany(List<Lite> lites, Type type, Enum operationKey, params object[] args);

        [OperationContract, NetDataContract]
        Dictionary<Enum, string> GetContextualCanExecute(Lite[] lites, List<Enum> cleanKeys);
    }
}
