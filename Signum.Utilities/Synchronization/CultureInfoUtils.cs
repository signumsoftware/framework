using System.Threading;
using System.Globalization;

namespace Signum.Utilities;

public static class CultureInfoUtils
{
    public static IDisposable? ChangeBothCultures(string? cultureName)
    {
        if (string.IsNullOrEmpty(cultureName))
            return null;

        return ChangeBothCultures(CultureInfo.GetCultureInfo(cultureName));
    }

    public static IDisposable? ChangeBothCultures(CultureInfo? ci)
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

    public static IDisposable? ChangeCulture(string? cultureName)
    {
        if (string.IsNullOrEmpty(cultureName))
            return null;

        return ChangeCulture(CultureInfo.GetCultureInfo(cultureName));
    }

    public static IDisposable? ChangeCulture(CultureInfo? ci)
    {
        if (ci == null)
            return null;

        Thread t = Thread.CurrentThread;
        CultureInfo old = t.CurrentCulture;
        t.CurrentCulture = ci;
        return new Disposable(() => t.CurrentCulture = old);
    }

    public static IDisposable? ChangeCultureUI(string cultureName)
    {
        if (string.IsNullOrEmpty(cultureName))
            return null;

        return ChangeCultureUI(CultureInfo.GetCultureInfo(cultureName));
    }

    public static IDisposable? ChangeCultureUI(CultureInfo? ci)
    {
        if (ci == null)
            return null;

        Thread t = Thread.CurrentThread;
        CultureInfo old = t.CurrentUICulture;
        t.CurrentUICulture = ci;
        return new Disposable(() => t.CurrentUICulture = old);
    }
}
