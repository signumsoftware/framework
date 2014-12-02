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
        List<Lite<UserChartEntity>> GetUserCharts(object queryName);

        [OperationContract, NetDataContract]
        List<Lite<UserChartEntity>> GetUserChartsEntity(Type entityType);

        [OperationContract, NetDataContract]
        List<Lite<UserChartEntity>> AutocompleteUserChart(string subString, int limit);

        [OperationContract, NetDataContract]
        UserChartEntity RetrieveUserChart(Lite<UserChartEntity> userChart);

        [OperationContract, NetDataContract]
        List<ChartScriptEntity> GetChartScripts();
    }

    [ServiceContract]
    public interface IResourcesByEntityServer
    {
        [OperationContract, NetDataContract]
        List<Lite<Entity>> GetResourcesByEntity(Type entityType);
    }
}
