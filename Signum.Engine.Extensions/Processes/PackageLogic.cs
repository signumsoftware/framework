using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Processes;
using Signum.Entities;

namespace Signum.Engine.Processes
{
    public static class PackageLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined<PackageDN>())
            {
                ProcessLogic.Start(sb, dqm); 

                sb.Include<PackageDN>();
                sb.Include<PackageLineDN>(); 

                if(!sb.Settings.IsTypeAttributesOverriden<IProcessData>())
                    sb.Settings.OverrideTypeAttributes<IProcessData>(new ImplementedByAttribute(typeof(PackageDN)));
            }
        }
    }
}
