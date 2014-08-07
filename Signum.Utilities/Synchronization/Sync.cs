using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.DataStructures;
using System.Threading;
using System.Globalization;
using System.Reflection;

namespace Signum.Utilities
{
    public static class Sync
    {
        public static IDisposable ChangeBothCultures(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                return null;

            return ChangeBothCultures(CultureInfo.GetCultureInfo(cultureName));
        }

        public static IDisposable ChangeBothCultures(CultureInfo ci)
        {
            if (ci == null)
                return null;

            Thread t = Thread.CurrentThread;
            CultureInfo old = t.CurrentCulture;
            CultureInfo oldUI = t.CurrentUICulture;
            t.CurrentCulture = ci;
            t.CurrentUICulture = ci;
            return new Disposable(() =>
            {
                t.CurrentCulture = old;
                t.CurrentUICulture = oldUI;
            });
        }

        public static IDisposable ChangeCulture(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                return null;

            return ChangeCulture(CultureInfo.GetCultureInfo(cultureName));
        }

        public static IDisposable ChangeCulture(CultureInfo ci)
        {
            if (ci == null)
                return null;

            Thread t = Thread.CurrentThread;
            CultureInfo old = t.CurrentCulture;
            t.CurrentCulture = ci;
            return new Disposable(() => t.CurrentCulture = old);
        }

        public static IDisposable ChangeCultureUI(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                return null;

            return ChangeCultureUI(CultureInfo.GetCultureInfo(cultureName));
        }

        public static IDisposable ChangeCultureUI(CultureInfo ci)
        {
            if (ci == null)
                return null;

            Thread t = Thread.CurrentThread;
            CultureInfo old = t.CurrentUICulture;
            t.CurrentUICulture = ci;
            return new Disposable(() => t.CurrentUICulture = old);
        }

        public static void SafeUpdate<T>(ref T variable, Func<T, T> repUpdateFunction) where T : class
        {
            T oldValue, newValue;
            do
            {
                oldValue = variable;
                newValue = repUpdateFunction(oldValue);

                if (newValue == null)
                    break;

            } while (Interlocked.CompareExchange<T>(ref variable, newValue, oldValue) != oldValue);
        }

        public static LocString ToLoc(Func<string> resourceProperty)
        {
            return lang =>
            {
                using (Sync.ChangeBothCultures(lang))
                    return resourceProperty();
            };
        }
    }

    public delegate string LocString(CultureInfo lang);
}
