using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Signum.Test
{
    public static class Assert2
    {
        public static void Throws<T>(Action action)
            where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new AssertFailedException("No {0} has been thrown".Formato(typeof(T).Name));
        }
    }
}
