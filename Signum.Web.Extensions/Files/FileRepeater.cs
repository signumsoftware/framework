#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections;
using System.Linq.Expressions;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System.Configuration;
using Signum.Web.Properties;
using Signum.Engine;
#endregion

namespace Signum.Web.Files
{
    public class FileRepeater : EntityRepeater
    {
        public FileRepeater(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {

        }
    }
}
