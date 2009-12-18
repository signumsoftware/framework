using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Operations;
using System.Web.Mvc;
using Signum.Entities;
using System.Web;

namespace Signum.Web.Operations
{
    public class ConstructorSettings : WebMenuItem
    {
        public Func<OperationInfo, HttpContextBase, IdentifiableEntity> Constructor { get; set; }
        public Func<OperationInfo, bool> IsVisible { get; set; }
    }

    public class EntityOperationSettings : WebMenuItem
    {
        public Func<IdentifiableEntity, bool> IsVisible { get; set; }
        public bool? MultiStep { get; set; }
        public bool? NavigateOnSuccess { get; set; }
        public bool? Post { get; set; }
    }

    public class ConstructorFromManySettings : WebMenuItem
    {
        public Func<ConstructorFromManyEventArgs, HttpContextBase, IdentifiableEntity> Constructor { get; set; }
        public Func<object, OperationInfo, bool> IsVisible { get; set; }
    }

    public class ConstructorFromManyEventArgs : EventArgs
    {
        public object QueryName { get; internal set; }
        public List<Lite> Entities { get; internal set; }
        public OperationInfo OperationInfo { get; internal set; }
    }
}
