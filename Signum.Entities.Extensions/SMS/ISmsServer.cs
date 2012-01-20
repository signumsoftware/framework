using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Services;

namespace Signum.Entities.SMS
{
    [ServiceContract]
    public interface ISmsServer
    {
        [OperationContract, NetDataContract]
        string GetPhoneNumber(IdentifiableEntity ie);

        [OperationContract, NetDataContract]
        List<string> GetLiteralsFromDataObjectProvider(TypeDN type);

        [OperationContract, NetDataContract]
        List<Lite> GetAssociatedTypesForTemplates();
    }
}
