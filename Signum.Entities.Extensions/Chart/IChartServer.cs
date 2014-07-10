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
        List<Lite<UserChartDN>> GetUserChartsEntity(Type entityType);

        [OperationContract, NetDataContract]
        List<Lite<UserChartDN>> AutocompleteUserChart(string subString, int limit);

        [OperationContract, NetDataContract]
        UserChartDN RetrieveUserChart(Lite<UserChartDN> userChart);

        [OperationContract, NetDataContract]
        List<ChartScriptDN> GetChartScripts();
    }

    [ServiceContract]
    public interface IResourcesByEntityServer
    {
        [OperationContract, NetDataContract]
        List<Lite<IdentifiableEntity>> GetResourcesByEntity(Type entityType);
    }
}
