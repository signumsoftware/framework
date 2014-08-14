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
    [ServiceContract]
    public interface IDiffLogServer
    {
        [OperationContract, NetDataContract]
        MinMax<OperationLogDN> OperationLogNextPrev(OperationLogDN log);
    }
}
