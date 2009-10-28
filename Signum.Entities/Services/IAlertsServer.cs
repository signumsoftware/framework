using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities.Basics;
using Signum.Entities;

namespace Signum.Services
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface IAlertsServer
    {
        [OperationContract, NetDataContract]
        List<Lite<IAlertDN>> RetrieveAlerts(Lite<IdentifiableEntity> lite);

        [OperationContract, NetDataContract]
        CountAlerts CountAlerts(Lite<IdentifiableEntity> lite);

        [OperationContract, NetDataContract]
        IAlertDN CheckAlert(IAlertDN alert);
    }
}
