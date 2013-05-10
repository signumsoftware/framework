using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;
using Signum.Entities.ControlPanel;

namespace Signum.Services
{
    [ServiceContract]
    public interface IControlPanelServer
    {
        [OperationContract, NetDataContract]
        ControlPanelDN GetHomePageControlPanel();

        [OperationContract, NetDataContract]
        List<Lite<ControlPanelDN>> GetControlPanelsEntity(Type entityType);

        [OperationContract, NetDataContract]
        List<Lite<ControlPanelDN>> AutocompleteControlPanel(string subString, int limit);
    }
}
