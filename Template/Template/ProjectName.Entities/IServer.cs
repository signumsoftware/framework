using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Services;

namespace $custommessage$.Services
{
    //Defines the WPF contract between client and server applications
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface IServer$custommessage$ : IBaseServer, IDynamicQueryServer //ILoginServer, 
    {

    }
}
