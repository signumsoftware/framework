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
    public interface IActionServer
    {
        [OperationContract, NetDataContract]
        List<ActionInfo> GetActionPairs(Lazy lazy);

        [OperationContract, NetDataContract]
        IdentifiableEntity ExecuteAction(IdentifiableEntity entity, Enum actionKey, object[] parameters);

        [OperationContract, NetDataContract]
        void ExecuteActionLazy(Lazy lazy, Enum actionKey, object[] parameters);
    }
}
