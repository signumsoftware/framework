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
        List<Lazy<IAlertDN>> RetrieveAlerts(Lazy<IdentifiableEntity> lazy);

        [OperationContract, NetDataContract]
        CountAlerts CountAlerts(Lazy<IdentifiableEntity> lazy);

        [OperationContract, NetDataContract]
        IAlertDN CheckAlert(IAlertDN alert);
    }
}
