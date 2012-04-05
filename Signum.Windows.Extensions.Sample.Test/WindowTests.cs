using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Signum.Windows.UIAutomation;
using System.Windows.Automation;
using Signum.Entities.DynamicQuery;
using Signum.Test;
using Signum.Entities;

namespace Signum.Windows.Extensions.Sample.Test
{
    [TestClass]
    public class WindowTests
    {
        [TestMethod]
        public void Search()
        {
            using (WindowProxy win = Common.StartAndLogin())
            {
                //win.Core.MenuItemInvoke("Music");
                using (SearchWindowProxy albums = new SearchWindowProxy(win.Element.MenuItemOpenWindow("Music", "Albumes")))
                {
                    albums.AddFilterString("Entity.Name", FilterOperation.Contains, "A");
                    albums.AddFilterString("Entity.Author", FilterOperation.EqualTo, "Smashing");

                    albums.Search();
                    using (NormalWindowProxy album = albums.ViewElementAt(0))
                    {


                    }
                }
            }

        }
    }
}
