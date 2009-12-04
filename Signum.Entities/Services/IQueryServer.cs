using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities.DynamicQuery;
using Signum.Entities;

namespace Signum.Services
{
    [ServiceContract]
    public interface IQueryServer
    {
        [OperationContract, NetDataContract]
        QueryDescription GetQueryDescription(object queryName);

        [OperationContract, NetDataContract]
        QueryResult GetQueryResult(object queryName, List<Filter> filters, int? limit);

        [OperationContract, NetDataContract]
        int GetQueryCount(object queryName, List<Filter> filters);

        [OperationContract, NetDataContract]
        Lite GetUniqueEntity(object queryName, List<Filter> filters, UniqueType uniqueType);

        [OperationContract, NetDataContract]
        List<object> GetQueryNames();      
    }
}
