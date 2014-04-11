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
        Dictionary<OperationSymbol, string> GetCanExecuteAll(IdentifiableEntity entity);

        [OperationContract, NetDataContract]
        Dictionary<OperationSymbol, string> GetCanExecuteLiteAll(Lite<IdentifiableEntity> lite);

        [OperationContract, NetDataContract]
        string GetCanExecute(IdentifiableEntity entity, OperationSymbol operationSymbol);

        [OperationContract, NetDataContract]
        string GetCanExecuteLite(Lite<IdentifiableEntity> lite, OperationSymbol operationSymbol);

        [OperationContract, NetDataContract]
        List<OperationInfo> GetOperationInfos(Type entityType);

        [OperationContract, NetDataContract]
        HashSet<Type> GetSaveProtectedTypes();

        [OperationContract, NetDataContract]
        IdentifiableEntity ExecuteOperation(IIdentifiable entity, OperationSymbol operationSymbol, params object[] args);

        [OperationContract, NetDataContract]
        IdentifiableEntity ExecuteOperationLite(Lite<IIdentifiable> lite, OperationSymbol operationSymbol, params object[] args);


        [OperationContract, NetDataContract]
        void Delete(Lite<IIdentifiable> lite, OperationSymbol operationSymbol, params object[] args);


        [OperationContract, NetDataContract]
        IdentifiableEntity Construct(Type type, OperationSymbol operationSymbol, params object[] args);


        [OperationContract, NetDataContract]
        IdentifiableEntity ConstructFrom(IIdentifiable entity, OperationSymbol operationSymbol, params object[] args);

        [OperationContract, NetDataContract]
        IdentifiableEntity ConstructFromLite(Lite<IIdentifiable> lite, OperationSymbol operationSymbol, params object[] args);


        [OperationContract, NetDataContract]
        IdentifiableEntity ConstructFromMany(IEnumerable<Lite<IIdentifiable>> lites, Type type, OperationSymbol operationSymbol, params object[] args);

        [OperationContract, NetDataContract]
        Dictionary<OperationSymbol, string> GetContextualCanExecute(Lite<IIdentifiable>[] lites, List<OperationSymbol> cleanKeys);
    }

}
