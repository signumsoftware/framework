using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities;

namespace Signum.Services
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface ILoginServer
    {
        [OperationContract, NetDataContract]
        void Login(string username, string passwordHash);

        [OperationContract(IsTerminating = false), NetDataContract]
        void Logout();
    }
}
