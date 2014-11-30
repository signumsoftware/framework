using Signum.Entities;
using Signum.Entities.Processes;
using Signum.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Signum.Services
{
    [ServiceContract]
    public interface IProcessServer
    {
        [OperationContract, NetDataContract]
        ProcessEntity CreatePackageOperation(IEnumerable<Lite<IEntity>> lites, OperationSymbol operationSymbol, params object[] operationArgs);
    }
}
