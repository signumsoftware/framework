using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Operations;
using System.Web.Mvc;
using Signum.Entities;

namespace Signum.Web.Operations
{
    //Constructor
    public class ConstructorSettings : WebMenuItem
    {
        public Func<OperationInfo, Controller, IdentifiableEntity> Constructor { get; set; }
        public Func<OperationInfo, bool> IsVisible { get; set; }
    }

    public class EntityOperationSettings : WebMenuItem
    {
        //public Func<EntityOperationEventArgs, IdentifiableEntity> Click { get; set; }
        public Func<IdentifiableEntity, bool> IsVisible { get; set; }
    }
}
