using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.ComponentModel;

namespace Signum.Utilities
{
    public static class AboutTools
    {
        public static string NiceWindowsVersion(this OperatingSystem os)
        {
            switch (os.Platform)
            {
                case PlatformID.Win32Windows:
                    switch (os.Version.Minor)
                    {
                        case 0: return "Windows 95";
                        case 10:
                            if (os.Version.Revision.ToString() == "2222A")
                                return "Windows 98 SE";
                            else
                                return "Windows 98";
                        case 90: return "Windows ME";
                    }
                    break;
                case PlatformID.Win32NT:
                    switch (os.Version.Major)
                    {
                        case 3: return "Windows NT 3.51";
                        case 4: return "Windows NT 4.0";
                        case 5:
                            switch (os.Version.Minor)
                            {
                                case 0: return "Windows 2000";
                                case 1: return "Windows XP";
                                case 2: return "Windows 2003 Server";
                            }
                            break;
                        case 6:
                            switch (os.Version.Minor)
                            {
                                case 0: return "Windows Vista / 2008 Server";
                                case 1: return "Windows 7";
                                case 2: return "Windows 8";
                            }
                            break;
                    }
                    break;
            }

            return AboutMessage.OS_Unknown0.NiceToString().FormatWith(os.VersionString);
        }

        public static DateTime CompilationTime(this Version v)
        {
            return new DateTime(v.Build * TimeSpan.TicksPerDay + v.Revision * TimeSpan.TicksPerSecond * 2).AddYears(1999).AddDays(-1);
        }

        public static List<NetFrameworkVersion> FrameworkVersions()
        {
            List<NetFrameworkVersion> versions = new List<NetFrameworkVersion>();
            RegistryKey oldComponentsKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Active Setup\Installed Components\{78705f0d-e8db-4b2d-8193-982bdda15ecd}\");
            if (oldComponentsKey == null)
                oldComponentsKey = Registry.LocalMachine.OpenSubKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\{FDC11A6F-17D1-48f9-9EA3-9051954BAA24}\");
            if (oldComponentsKey != null)
                versions.AddRange(oldComponentsKey.SubKeys((n, k) =>
                    new NetFrameworkVersion()
                    {
                        GlobalVersion = "v1.0",
                        FullVersion = (string)k.GetValue("Version"),
                        ServicePack = GetOldComponentServicePack(k)
                    }).ToList());


            RegistryKey componentsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Net Framework Setup\NDP\");
            versions.AddRange(componentsKey.SubKeys((n, k) =>
                new NetFrameworkVersion
                {
                    GlobalVersion = n,
                    FullVersion = (string)k.GetValue("Version"),
                    ServicePack = (int?)k.GetValue("SP")
                }).ToList());
            return versions;
        }

        private static int? GetOldComponentServicePack(RegistryKey k)
        {
            string[] e = ((string)k.GetValue("Version")).Split(',');
            if (int.TryParse(e.LastOrDefault(), out int sp))
                return Extensions.DefaultToNull(sp);
            else
                return null;
        }

        public static IEnumerable<T> SubKeys<T>(this RegistryKey key, Func<string, RegistryKey, T> func)
        {
            string[] keyNames = key.GetSubKeyNames();
            foreach (var keyName in keyNames)
            {
                using (var k = key.OpenSubKey(keyName))
                    yield return func(keyName, k);
            }
        }

        public class NetFrameworkVersion
        {
            public string GlobalVersion { get; internal set; }
            public string FullVersion { get; internal set; }
            public int? ServicePack { get; internal set; }

            public override string ToString()
            {
                return GlobalVersion + (ServicePack != null ? " SP" + ServicePack : "");
            }
        }
    }

    public enum AboutMessage
    {
        [Description("Unknown ({0})")]
        OS_Unknown0
    }
}
