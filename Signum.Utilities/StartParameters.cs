using Signum.Utilities.ExpressionTrees;
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


        public static List<R> SelectCatch<T, R>(this IEnumerable<T> elements, Func<T, R> selectorOrCrash)
        {
            List<R> result = new List<R>();
            foreach (var e in elements)
            {
                try
                {
                    result.Add(selectorOrCrash(e));
                }
                catch (Exception ex) when (StartParameters.IgnoredDatabaseMismatches != null)
                {
                    //This try { throw } catch is here to alert developers.
                    //In production, in some cases its OK to attempt starting an application with a slightly different schema (dynamic entities, green-blue deployments).  
                    //In development, consider synchronize.  
                    StartParameters.IgnoredDatabaseMismatches.Add(ex);
                    continue;
                }
            }
            return result;
        }
    }
}
