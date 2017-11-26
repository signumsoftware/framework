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
    public interface IDynamicQueryServer
    {
        [OperationContract, NetDataContract]
        QueryDescription GetQueryDescription(object queryName);

        [OperationContract, NetDataContract]
        ResultTable ExecuteQuery(QueryRequest request);

        [OperationContract, NetDataContract]
        int ExecuteQueryCount(QueryValueRequest request);

        [OperationContract, NetDataContract]
        Lite<Entity> ExecuteUniqueEntity(UniqueEntityRequest request);

        [OperationContract, NetDataContract]
        object[] BatchExecute(BaseQueryRequest[] requests);

        [OperationContract, NetDataContract]
        List<object> GetQueryNames();

        [OperationContract, NetDataContract]
        List<QueryToken> ExternalQueryToken(QueryToken parent);
    }

}
