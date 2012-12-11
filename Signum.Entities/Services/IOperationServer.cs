using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities;
using Signum.Entities.Basics;

namespace Signum.Services
{
    [ServiceContract]
    public interface IOperationServer
    {
        [OperationContract, NetDataContract]
        Dictionary<Enum, string> GetCanExecute(IdentifiableEntity entity);

        [OperationContract, NetDataContract]
        Dictionary<Enum, string> GetCanExecuteLite(Lite lite);

        [OperationContract, NetDataContract]
        List<OperationInfo> GetOperationInfos(Type entityType);

        [OperationContract, NetDataContract]
        HashSet<Type> GetSaveProtectedTypes();

        [OperationContract, NetDataContract]
        IdentifiableEntity ExecuteOperation(IIdentifiable entity, Enum operationKey, params object[] args);

        [OperationContract, NetDataContract]
        IdentifiableEntity ExecuteOperationLite(Lite lite, Enum operationKey, params object[] args);


        [OperationContract, NetDataContract]
        IdentifiableEntity Delete(Lite lite, Enum operationKey, params object[] args);


        [OperationContract, NetDataContract]
        IdentifiableEntity Construct(Type type, Enum operationKey, params object[] args);


        [OperationContract, NetDataContract]
        IdentifiableEntity ConstructFrom(IIdentifiable entity, Enum operationKey, params object[] args);

        [OperationContract, NetDataContract]
        IdentifiableEntity ConstructFromLite(Lite lite, Enum operationKey, params object[] args);


        [OperationContract, NetDataContract]
        IdentifiableEntity ConstructFromMany(List<Lite> lites, Type type, Enum operationKey, params object[] args);

        [OperationContract, NetDataContract]
        Dictionary<Enum, string> GetContextualCanExecute(Lite[] lites, List<Enum> cleanKeys);
    }

}
