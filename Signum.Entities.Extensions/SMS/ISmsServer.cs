using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Services;

namespace Signum.Entities.Extensions.SMS
{
    [ServiceContract]
    public interface ISmsServer
    {
        [OperationContract, NetDataContract]
        string GetPhoneNumber(IdentifiableEntity ie);
    }
}
