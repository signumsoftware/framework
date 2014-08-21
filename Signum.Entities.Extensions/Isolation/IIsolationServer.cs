using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Chart;
using Signum.Entities;
using Signum.Entities.Isolation;

namespace Signum.Services
{
    [ServiceContract]
    public interface IIsolationServer
    {
        [OperationContract, NetDataContract]
        Lite<IsolationDN> GetOnlyIsolation(List<Lite<IdentifiableEntity>> selectedEntities);
    }
}
