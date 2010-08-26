using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Chart;
using Signum.Entities;

namespace Signum.Services
{
    [ServiceContract]
    public interface IChartServer
    {
        [OperationContract, NetDataContract]
        ResultTable ExecuteChart(ChartRequest request);

        [OperationContract, NetDataContract]
        List<Lite<UserChartDN>> GetUserCharts(object queryName);

        [OperationContract, NetDataContract]
        void RemoveUserChart(Lite<UserChartDN> lite);
    }

    //public ResultTable ExecuteChart(ChartRequest request)
    //{
    //    return Return(MethodInfo.GetCurrentMethod(),
    //    () => ChartLogic.ExecuteChart(request));
    //}

    //public List<Lite<UserChartDN>> GetUserCharts(object queryName)
    //{
    //    return Return(MethodInfo.GetCurrentMethod(),
    //    () => ChartLogic.GetUserCharts(queryName));
    //}

    //public void RemoveUserChart(Lite<UserChartDN> lite)
    //{
    //    Execute(MethodInfo.GetCurrentMethod(),
    //      () => ChartLogic.RemoveUserChart(lite));
    //}
}
