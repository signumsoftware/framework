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
    public abstract class OperationSettings : WebMenuItem
    { 
        public JsOperationOptions Options { get; set; }
    }

    public class ConstructorSettings : OperationSettings
    {
        public Func<OperationInfo, HttpContextBase, IdentifiableEntity> Constructor { get; set; }
        public Func<OperationInfo, bool> IsVisible { get; set; }
    }

    public class EntityOperationSettings : OperationSettings
    {
        public Func<IdentifiableEntity, bool> IsVisible { get; set; }
        public bool? Post { get; set; }
    }

    public class ConstructorFromManySettings : OperationSettings
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
