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
        Dictionary<OperationSymbol, string> GetCanExecuteAll(Entity entity);

        [OperationContract, NetDataContract]
        Dictionary<OperationSymbol, string> GetCanExecuteLiteAll(Lite<Entity> lite);

        [OperationContract, NetDataContract]
        string GetCanExecute(Entity entity, OperationSymbol operationSymbol);

        [OperationContract, NetDataContract]
        string GetCanExecuteLite(Lite<Entity> lite, OperationSymbol operationSymbol);

        [OperationContract, NetDataContract]
        List<OperationInfo> GetOperationInfos(Type entityType);
         
        [OperationContract, NetDataContract]
        bool HasConstructOperations(Type entityType);

        [OperationContract, NetDataContract]
        Entity ExecuteOperation(IEntity entity, OperationSymbol operationSymbol, params object[] args);

        [OperationContract, NetDataContract]
        Entity ExecuteOperationLite(Lite<IEntity> lite, OperationSymbol operationSymbol, params object[] args);

        [OperationContract, NetDataContract]
        void DeleteLite(Lite<IEntity> lite, OperationSymbol operationSymbol, params object[] args);

        [OperationContract, NetDataContract]
        void Delete(IEntity entity, OperationSymbol operationSymbol, params object[] args);

        [OperationContract, NetDataContract]
        Entity Construct(Type type, OperationSymbol operationSymbol, params object[] args);


        [OperationContract, NetDataContract]
        Entity ConstructFrom(IEntity entity, OperationSymbol operationSymbol, params object[] args);

        [OperationContract, NetDataContract]
        Entity ConstructFromLite(Lite<IEntity> lite, OperationSymbol operationSymbol, params object[] args);


        [OperationContract, NetDataContract]
        Entity ConstructFromMany(IEnumerable<Lite<IEntity>> lites, Type type, OperationSymbol operationSymbol, params object[] args);

        [OperationContract, NetDataContract]
        Dictionary<OperationSymbol, string> GetContextualCanExecute(IEnumerable<Lite<IEntity>> lites, List<OperationSymbol> cleanKeys);
    }
}
