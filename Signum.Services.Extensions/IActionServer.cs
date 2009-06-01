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
        List<OperationInfo> GetOperationsInfo(Lazy lazy);

        [OperationContract, NetDataContract]
        IdentifiableEntity ExecuteOperation(IdentifiableEntity entity, Enum operationKey, params object[] parameters);

        [OperationContract, NetDataContract]
        IdentifiableEntity ExecuteOperationLazy(Lazy lazy, Enum operationKey, params object[] parameters);
    }
}
