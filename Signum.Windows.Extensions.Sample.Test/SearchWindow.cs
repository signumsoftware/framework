using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Signum.Windows.UIAutomation;
using System.Windows.Automation;
using Signum.Entities.DynamicQuery;

namespace Signum.Windows.Extensions.Sample.Test
{
    [TestClass]
    public class SearchWindow
    {
        [TestMethod]
        public void Search()
        {
            using (WindowProxy win = Common.StartAndLogin())
            {
                //win.Core.MenuItemInvoke("Music");
                using (SearchWindowProxy albums = new SearchWindowProxy(win.Owner.MenuItemOpenWindow("Music", "Albumes")))
                {
                    albums.AddFilter("Entity.Name", FilterOperation.Contains, "Olmo");

                    albums.Search();
                    using (NormalWindowProxy album = albums.OpenItemByIndex(0))
                    {


                    }
                }
            }

        }
    }
}
