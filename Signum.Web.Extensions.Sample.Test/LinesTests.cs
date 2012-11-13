using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Web.Selenium;
using Selenium;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Authorization;
using Signum.Test;
using Signum.Web.Extensions.Sample.Test.Properties;
using Signum.Engine.Maps;
using Signum.Engine.Authorization;
using Signum.Utilities;

namespace Signum.Web.Extensions.Sample.Test
{
    [TestClass]
    public class LinesTests : Common
    {
        const string grammyAward = "GrammyAward";

        public LinesTests()
        {

        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            Common.Start();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Common.MyTestCleanup();
        }

        [TestMethod]
        public void Lines001_EntityLine()
        {
            CheckLoginAndOpen(ViewRoute("Band", 1));

            string prefix = "LastAward_";

            //view
            selenium.LineView(prefix);

            //cancel 
            selenium.PopupCancel(prefix);

            //delete
            selenium.LineRemove(prefix);
            selenium.EntityLineHasValue(prefix, false);

            //create with implementations
            selenium.LineCreateWithImpl(prefix, true, grammyAward);
            selenium.Type("{0}Category".Formato(prefix), "test");
            selenium.PopupOk(prefix);
            selenium.EntityLineHasValue(prefix, true);

            //find with implementations
            selenium.LineRemove(prefix);
            selenium.LineFindWithImplAndSelectElements(prefix, grammyAward, false, new int[] { 0 });
            selenium.EntityLineHasValue(prefix, true);
        }

        [TestMethod]
        public void Lines002_EntityLineInPopup()
        {
            CheckLoginAndOpen(ViewRoute("Album", 1));

            //open popup
            selenium.LineView("Author_");

            string prefix = "Author_LastAward_";

            //view
            selenium.LineView(prefix);
            selenium.PopupOk(prefix);

            //delete
            selenium.LineRemove(prefix);
            selenium.EntityLineHasValue(prefix, false);

            //create with implementations
            selenium.LineCreateWithImpl(prefix, true, "AmericanMusicAward");
            selenium.Type("{0}Category".Formato(prefix), "test");
            selenium.PopupOk(prefix);
            selenium.EntityLineHasValue(prefix, true);

            //find with implementations
            selenium.LineRemove(prefix);
            selenium.LineFindWithImplAndSelectElements(prefix, "AmericanMusicAward", false, new int[] { 0 });
            selenium.EntityLineHasValue(prefix, true);
        }

        [TestMethod]
        public void Lines003_EntityLineDetail()
        {
            CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Music/BandDetail");

            string prefix = "LastAward_";

            //Value is opened by default
            selenium.EntityLineDetailHasValue(prefix, true);

            //Delete
            selenium.LineRemove(prefix);
            selenium.EntityLineDetailHasValue(prefix, false);

            //create with implementations
            selenium.LineCreateWithImpl(prefix, false, "AmericanMusicAward");
            selenium.WaitAjaxFinished(() => selenium.CheckEntityListDetailHasValue(prefix, true));
            selenium.Type("{0}Category".Formato(prefix), "test");

            //find with implementations
            selenium.LineRemove(prefix);
            selenium.LineFindWithImplAndSelectElements(prefix, "AmericanMusicAward", false, new int[]{0});
            selenium.WaitAjaxFinished(() => selenium.CheckEntityListDetailHasValue(prefix, true));
        }

        [TestMethod]
        public void Lines004_EntityList()
        {
            CheckLoginAndOpen(ViewRoute("Band", 1));

            string prefix = "Members_";

            //Create and cancel
            selenium.LineCreate(prefix, true, 4);
            selenium.PopupCancelDiscardChanges("{0}4_".Formato(prefix));
            selenium.ListLineElementExists(prefix, 4, false);

            //Create and ok
            selenium.LineCreate(prefix, true, 4);
            selenium.Type("{0}4_Name".Formato(prefix), "test");
            selenium.PopupOk("{0}4_".Formato(prefix));
            selenium.ListLineElementExists(prefix, 4, true);
            Assert.IsTrue(selenium.IsElementPresent("jq=#Members_4_sfEntity"));
            
            //Delete
            selenium.LineRemove(prefix);
            selenium.ListLineElementExists(prefix, 4, false);

            //Find multiple
            selenium.LineFindAndSelectElements(prefix, true, new int[] { 4, 5 }, 4);
            selenium.ListLineElementExists(prefix, 4, true);
            selenium.ListLineElementExists(prefix, 5, true);

            prefix = "OtherAwards_";

            //Create with implementations
            selenium.LineCreateWithImpl(prefix, true, grammyAward, 0);
            selenium.Type("{0}0_Category".Formato(prefix), "test");
            selenium.PopupOk("{0}0_".Formato(prefix));
            selenium.ListLineElementExists(prefix, 0, true);
            Assert.IsTrue(selenium.IsElementPresent("jq=#OtherAwards_0_sfEntity"));

            //find with implementations
            selenium.LineFindWithImplAndSelectElements(prefix, grammyAward, true, new int[] { 0 }, 1);
            selenium.ListLineElementExists(prefix, 1, true);

            //Delete
            selenium.LineRemove(prefix);
            selenium.ListLineElementExists(prefix, 1, false);

            //View
            selenium.ListLineViewElement(prefix, 0, true);
            selenium.PopupCancelDiscardChanges("{0}0_".Formato(prefix));
        }

        [TestMethod]
        public void Lines005_EntityListInPopup()
        {
            CheckLoginAndOpen(ViewRoute("Band", 1));

            //open popup
            selenium.ListLineViewElement("Members_", 0, true);

            string prefix = "Members_0_Friends_";

            //create
            selenium.LineCreate(prefix, true, 1);

            selenium.Type("{0}1_Name".Formato(prefix), "test");
            selenium.PopupOk("{0}1_".Formato(prefix));
            selenium.ListLineElementExists(prefix, 1, true);

            //find multiple
            selenium.LineFindAndSelectElements(prefix, true, new int[]{4,5}, 2);
            selenium.ListLineElementExists(prefix, 2, true);
            selenium.ListLineElementExists(prefix, 3, true);

            //delete multiple
            selenium.ListLineSelectElement(prefix, 1, false);
            selenium.ListLineSelectElement(prefix, 2, true);
            selenium.LineRemove(prefix);
            selenium.LineRemove(prefix);
            selenium.ListLineElementExists(prefix, 1, false);
            selenium.ListLineElementExists(prefix, 2, false);
            selenium.ListLineElementExists(prefix, 3, true);
        }

        [TestMethod]
        public void Lines006_EntityListDetail()
        {
            CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Music/BandDetail");

            string prefix = "Members_";

            //1st element is shown by default
            selenium.EntityListDetailHasValue(prefix, true);

            //create
            selenium.LineCreate(prefix, false, 4);
            selenium.WaitAjaxFinished(() => selenium.CheckEntityListDetailHasValue(prefix, true));
            selenium.Type("{0}4_Name".Formato(prefix), "test");
            selenium.ListLineElementExists(prefix, 4, true);

            //delete
            selenium.LineRemove(prefix);
            selenium.ListLineElementExists(prefix, 4, false);
            selenium.EntityListDetailHasValue(prefix, false);

            //find multiple
            selenium.LineFindAndSelectElements(prefix, true, new int[] { 4, 5 }, 4);
            selenium.ListLineElementExists(prefix, 4, true);
            selenium.ListLineElementExists(prefix, 5, true);
            selenium.EntityListDetailHasValue(prefix, true);

            prefix = "OtherAwards_";

            //create with implementations
            selenium.LineCreateWithImpl(prefix, false, grammyAward, 0);
            selenium.WaitAjaxFinished(() => selenium.CheckEntityListDetailHasValue(prefix, true));
            selenium.ListLineElementExists(prefix, 0, true);
            selenium.Type("{0}0_Category".Formato(prefix), "test");

            //find with implementations
            selenium.LineFindWithImplAndSelectElements(prefix, grammyAward, true, new int[] { 0 }, 1);
            selenium.ListLineElementExists(prefix, 1, true);
            selenium.WaitAjaxFinished(() => selenium.CheckEntityListDetailHasValue(prefix, true));

            //Delete
            selenium.LineRemove(prefix);
            selenium.ListLineElementExists(prefix, 1, false);

            //View detail
            selenium.ListLineViewElement(prefix, 0, false);
            selenium.WaitAjaxFinished(() => selenium.CheckEntityListDetailHasValue(prefix, true));
        }

        [TestMethod]
        public void Lines007_EntityRepeater()
        {
            CheckLoginAndOpen("/Signum.Web.Extensions.Sample/Music/BandRepeater");

            string prefix = "Members_";

            //All elements are shown
            selenium.RepeaterItemExists(prefix, 0, true);
            selenium.RepeaterItemExists(prefix, 1, true);
            selenium.RepeaterItemExists(prefix, 2, true);
            selenium.RepeaterItemExists(prefix, 3, true);
            
            //Create
            selenium.LineCreate(prefix, false, 4);
            selenium.RepeaterWaitUntilItemLoaded(prefix, 4);
            selenium.Type("{0}4_Name".Formato(prefix), "test");
            selenium.RepeaterItemExists(prefix, 4, true);

            //delete new element (created in client)
            selenium.LineRemove("{0}4_".Formato(prefix));
            selenium.RepeaterItemExists(prefix, 4, false);

            //delete old element (created in server)
            selenium.LineRemove("{0}0_".Formato(prefix));
            selenium.RepeaterItemExists(prefix, 0, false);

            //find multiple: it exists because Find is overriden to true in this EntityRepeater
            selenium.LineFindAndSelectElements(prefix, true, new int[]{4,5}, 4);
            selenium.RepeaterItemExists(prefix, 4, true);
            selenium.RepeaterItemExists(prefix, 5, true);

            //move up
            string secondItemMichael = "jq=#Members_4_sfIndexes[value=';2']";
            string thirdItemMichael = "jq=#Members_4_sfIndexes[value=';3']";
            Assert.IsTrue(!selenium.IsElementPresent(secondItemMichael) && !selenium.IsElementPresent(thirdItemMichael));
            selenium.RepeaterItemMove(true, prefix, 4);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(thirdItemMichael));
            //move down
            selenium.RepeaterItemMove(false, prefix, 2);
            selenium.WaitAjaxFinished(() => 
                selenium.IsElementPresent(secondItemMichael) && 
                !selenium.IsElementPresent(thirdItemMichael));

            prefix = "OtherAwards_";

            //create with implementations
            selenium.LineCreateWithImpl(prefix, false, grammyAward, 0);
            selenium.RepeaterItemExists(prefix, 0, true);
            selenium.Type("{0}0_Category".Formato(prefix), "test");

            //find does not exist by default
            Assert.IsFalse(selenium.IsElementPresent(LinesTestExtensions.LineFindSelector(prefix)));
        }
    }
}
