using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Media.Imaging;
using Signum.Entities;
using Signum.Services;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Windows.Basics;
using Signum.Entities.Basics;

namespace Signum.Windows
{
    public static class DateSpanClient
    {
        public static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(() => DateSpanClient.Start()));
        }

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EmbeddedEntitySettings<DateSpanDN>
                {
                    View = e => new DateSpan()
                });
            }
        }
    }
}
