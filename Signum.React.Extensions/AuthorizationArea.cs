using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.React.Authorization
{
    public class AssemblyAreasExtensions
    {
        public static void Register(string areaName)
        {
            AssemblyAreas.RegisterArea(typeof(AssemblyAreasExtensions), areaName, "Signum.React." + areaName, "Signum.React." + areaName);
        }
    }
}