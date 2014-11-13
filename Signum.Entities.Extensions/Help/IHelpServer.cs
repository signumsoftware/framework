using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities;

namespace Signum.Services
{
    [ServiceContract]
    public interface IHelpServer
    {
        [OperationContract, NetDataContract]
        bool HasEntityHelpService(Type type);

        [OperationContract, NetDataContract]
        bool HasQueryHelpService(object queryName);

        [OperationContract, NetDataContract]
        EntityHelpService GetEntityHelpService(Type type);

        [OperationContract, NetDataContract]
        QueryHelpService GetQueryHelpService(object queryName);

        [OperationContract, NetDataContract]
        Dictionary<PropertyRoute, HelpToolTipInfo> GetPropertyRoutesService(List<PropertyRoute> route);
    }

    [Serializable]
    public class EntityHelpService
    {
        public Type Type { get; set; }
        public HelpToolTipInfo Info { get; set; }
        public Dictionary<PropertyRoute, HelpToolTipInfo> Properties { get; set; }
        public Dictionary<OperationSymbol, HelpToolTipInfo> Operations { get; set; }
    }

    [ServiceContract]
    public class QueryHelpService
    {
        public object QueryName { get; set; }
        public HelpToolTipInfo Info { get; set; }
        public Dictionary<string, HelpToolTipInfo> Columns { get; set; }
    }

    public class HelpToolTipInfo
    {
        public string Title { get; set; }
        public string Info { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
    }
}
