using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Media.Imaging;
using Signum.Entities;
using Signum.Services;
using System.Reflection;
using Signum.Entities.Extensions.Basics;
using Signum.Utilities.Reflection;
using Signum.Windows.Basics;

namespace Signum.Windows
{
    public static class DateSpanClient
    {
        internal static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(() => DateSpanClient.Start()));
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
