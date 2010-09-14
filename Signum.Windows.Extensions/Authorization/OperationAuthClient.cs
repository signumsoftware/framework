using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Windows.Authorization
{
    public static class OperationAuthClient
    {
        public static bool Started { get; private set; }

        internal static void Start()
        {
            Started = true;
        }
    }
}

