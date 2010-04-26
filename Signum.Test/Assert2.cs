using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Test.Properties;

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

            throw new AssertFailedException(Resources.No0HasBeenThrown.Formato(typeof(T).Name));
        }

        public static void Throws<T>(Action action, string messageToContain)
           where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if(!ex.Message.Contains(messageToContain))
                    throw new AssertFailedException(Resources.ExceptionThrownDoesNotContainMessage0.Formato(ex.Message));

                return;
            }

            throw new AssertFailedException(Resources.No0HasBeenThrown.Formato(typeof(T).Name));
        }
    }
}
