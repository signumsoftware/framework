using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;
using System.Xml.Linq;

namespace Signum.Services
{
    [ServiceContract]
    public interface IUserQueryServer
    {
        [OperationContract, NetDataContract]
        List<Lite<UserQueryEntity>> GetUserQueries(object queryName);

        [OperationContract, NetDataContract]
        List<Lite<UserQueryEntity>> GetUserQueriesEntity(Type entityType);

        [OperationContract, NetDataContract]
        List<Lite<UserQueryEntity>> AutocompleteUserQueries(string subString, int limit);

        [OperationContract, NetDataContract]
        UserQueryEntity RetrieveUserQuery(Lite<UserQueryEntity> userQuery);
    }
}
