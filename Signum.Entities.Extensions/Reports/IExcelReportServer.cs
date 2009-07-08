using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reports;

namespace Signum.Services
{
    [ServiceContract]
    public interface IExcelReportServer
    {
        [OperationContract, NetDataContract]
        List<Lazy<ExcelReportDN>> GetExcelReports(string queryName);

        [OperationContract, NetDataContract]
        List<Lazy<ExcelReportDN>> GetEnabledExcelReports(string queryName);
    }
}
