using System;
using System.Reflection;
using System.Windows;

namespace Signum.Windows
{
    public static class ThemeManager
    {
        #region Fields

        private const BindingFlags DefaultStaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags DefaultInstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly static Assembly PresentationFramework = typeof(FrameworkElement).Assembly;
        
        private readonly static Type UxThemeWrapper = PresentationFramework.GetType("MS.Win32.UxThemeWrapper");
        private readonly static Type SystemResources = PresentationFramework.GetType("System.Windows.SystemResources");

        private readonly static MethodInfo SystemResources_InvalidateResources = SystemResources.GetMethod("InvalidateResources", DefaultStaticFlags);
        private readonly static MethodInfo SystemResources_OnThemeChanged = SystemResources.GetMethod("OnThemeChanged", DefaultStaticFlags);
        private readonly static MethodInfo SystemParameters_InvalidateCache = typeof(SystemParameters).GetMethod("InvalidateCache", DefaultStaticFlags, null, Type.EmptyTypes, null);
        private readonly static MethodInfo SystemColors_InvalidateCache = typeof(SystemColors).GetMethod("InvalidateCache", DefaultStaticFlags);

        private readonly static FieldInfo UxThemeWrapper_isActive = UxThemeWrapper.GetField("_isActive", DefaultStaticFlags);
        private readonly static FieldInfo UxThemeWrapper_themeColor = UxThemeWrapper.GetField("_themeColor", DefaultStaticFlags);
        private readonly static FieldInfo UxThemeWrapper_themeName = UxThemeWrapper.GetField("_themeName", DefaultStaticFlags);

        #endregion

        #region Intercept Theme Change

        static ThemeManager()
        {
            SystemResources.GetMethod("EnsureResourceChangeListener", DefaultStaticFlags).Invoke(null, null);

            Delegate hook = Delegate.CreateDelegate(typeof(DependencyObject).Assembly.GetType("MS.Win32.HwndWrapperHook"),
                typeof(ThemeManager).GetMethod("FilterThemeMessage", DefaultStaticFlags));

            object notify = SystemResources.GetField("_hwndNotify", DefaultStaticFlags).GetValue(null);
            notify = notify.GetType().GetProperty("Value", DefaultInstanceFlags).GetValue(notify, null);
            notify.GetType().GetMethod("AddHook", DefaultInstanceFlags).Invoke(notify, new object[] { hook });
        }

        private static IntPtr FilterThemeMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x31a) // WM_THEMECHANGED
            {
                handled = true;
            }
            return IntPtr.Zero;
        }

        #endregion

        #region Change Theme

        public const string Default = null;
        public const string Classic = "classic";
        public const string LunaNormalcolor = "luna.normalcolor";
        public const string LunaHomestead = "luna.homestead";
        public const string LunaMetallic = "luna.metallic";
        public const string RoyaleNormalcolor = "royale.normalcolor";
        public const string AeroNormalcolor = "aero.normalcolor";

        /// <summary>
        /// Changes the theme using a compound theme name (theme-name[.theme-color]).
        /// </summary>
        /// <param name="compoundThemeName">Compound theme name.</param>
        public static void ChangeTheme(string compoundThemeName)
        {
            if (string.IsNullOrEmpty(compoundThemeName))
            {
                ChangeTheme(string.Empty, string.Empty);
            }
            else
            {
                string[] themeData = compoundThemeName.Split('.');
                if (themeData.Length == 1)
                {
                    ChangeTheme(themeData[0], string.Empty);
                }
                else
                {
                    ChangeTheme(themeData[0], themeData[1]);
                }
            }
        }

        /// <summary>
        /// Changes the theme.
        /// </summary>
        /// <param name="themeName">Name of the theme.</param>
        /// <param name="themeColor">Color of the theme.</param>
        public static void ChangeTheme(string themeName, string themeColor)
        {
            SystemColors_InvalidateCache.Invoke(null, null);
            SystemParameters_InvalidateCache.Invoke(null, null);

            SystemResources_OnThemeChanged.Invoke(null, null);

            if (!string.IsNullOrEmpty(themeName))
            {
                UxThemeWrapper_isActive.SetValue(null, true);
                UxThemeWrapper_themeName.SetValue(null, themeName);
                UxThemeWrapper_themeColor.SetValue(null, themeColor);
            }

            SystemResources_InvalidateResources.Invoke(null, new object[] { false });
        }

        #endregion
    }
}
