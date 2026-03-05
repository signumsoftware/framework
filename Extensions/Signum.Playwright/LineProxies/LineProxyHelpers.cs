using System.Reflection;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Helper extensions for Line Proxies
/// </summary>
internal static class LineProxyHelpers
{
    public static bool IsNumericType(this Type type)
    {
        return type == typeof(int) ||
               type == typeof(long) ||
               type == typeof(short) ||
               type == typeof(byte) ||
               type == typeof(decimal) ||
               type == typeof(double) ||
               type == typeof(float) ||
               type == typeof(uint) ||
               type == typeof(ulong) ||
               type == typeof(ushort) ||
               type == typeof(sbyte);
    }

    public static bool IsAssignableFromGenericDefinition(this Type type, Type genericDefinition)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinition)
            return true;

        if (type.BaseType != null && type.BaseType.IsAssignableFromGenericDefinition(genericDefinition))
            return true;

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == genericDefinition)
                return true;
        }

        return false;
    }

    public static async Task<BaseLineProxy> AutoLineAsync(ILocator element, PropertyRoute route, IPage page)
    {
        return BaseLineProxy.AutoLine(element, route, page);
    }
}
