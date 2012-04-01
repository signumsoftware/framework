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
                    albums.SelectToken("Entity.Name");
                    var filter = albums.AddFilter();
                    filter.SetOperation(FilterOperation.Contains);
                    albums.Search();
                    using (NormalWindowProxy album = albums.OpenItemByIndex(0))
                    {


                    }
                    //win.Core.ChildById("txtA").Pattern<ValuePattern>().SetValue("10");
                    //win.Core.ChildById("txtB").Pattern<ValuePattern>().SetValue("5");

                }
            }

        }
    }
}
