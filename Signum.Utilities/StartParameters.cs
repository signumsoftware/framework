using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Utilities
{
    public static class StartParameters
    {
        //The best development experience is to throw an exception when starting but in some scenarios is better to try to start at all costs:

        // * Green / Blue deployments where the Application and the DB could be mistmatched for a while     
        public static List<Exception> IgnoredDatabaseMismatches; //Initialize to enable

        // * Dynamic code where everithing could happen (like duplicated entities, entities without operations, etc..)
        public static List<Exception> IgnoredCodeErrors;//Initialize to enable
    }
}
