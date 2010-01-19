using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Media.Imaging;
using Signum.Entities;
using Signum.Services;
using System.Reflection;
using Signum.Entities.Extensions.Basics;
using Signum.Windows.Extensions.Basics;
using Signum.Utilities.Reflection;

namespace Signum.Windows.Processes
{
    public static class BasicClient
    {
        internal static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(() => BasicClient.Start()));
        }

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.Manager.Settings.Add(typeof(DateSpanDN), new EntitySettings(EntityType.Default) { ViewEmbedded = (e, tc) => new DateSpan(tc) });
            }
        }
    }
}
