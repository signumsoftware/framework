#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web.Properties;
#endregion

namespace Signum.Web
{
    public static class EntityListBaseKeys
    {
        public const string Index = "sfIndex";
    }

    public abstract class EntityListBase : EntityBase
    {
        public EntityListBase() { }

        public override void SetReadOnly()
        {
            Find = false;
            Create = false;
            Remove = false;
            Implementations = null;
        }
    }
}
