using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Services;
using Signum.Entities.Basics;
using System.ServiceModel;

namespace Signum.Test.Extensions
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface IServerSample : 
        IBaseServer, IDynamicQueryServer, ILoginServer, IOperationServer, IQueryServer, IChartServer, IUserQueryServer,
        IQueryAuthServer, IPropertyAuthServer, ITypeAuthServer, IFacadeMethodAuthServer, IPermissionAuthServer, IOperationAuthServer,
        IEntityGroupAuthServer, IExcelReportServer
    {
    }
}
