using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Chart;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Services
{
    [ServiceContract]
    public interface IProfilerServer
    {
        [OperationContract, NetDataContract]
        void PushProfilerEntries(List<HeavyProfilerEntry> entries);
    }
}
