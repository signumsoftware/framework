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
using System.Text.RegularExpressions;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Web.Extensions.Sample.Test
{
    [TestClass]
    public class SearchControlTests : Common
    {
        public SearchControlTests()
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
        public void SearchControl001_Filters()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            //No filters
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(12));

            //Quickfilter of a Lite
            selenium.QuickFilter(1, 4, 0);
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(4));

            //Filter of a Lite from the combo
            selenium.FilterSelectToken(0, "label=Label", true);
            selenium.AddFilter(1);
            OpenFinderAndSelectFirstOrderedById(selenium, "value_1_");
            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("{0}:visible".Formato(LinesTestExtensions.LineFindSelector("value_1_"))));
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(2));
            selenium.DeleteFilter(1);

            //Filter from the combo with Subtokens
            selenium.FilterSelectToken(0, "label=Label", true);
            selenium.FilterSelectToken(1, "label=Name", false);
            selenium.AddFilter(1);
            selenium.Type("value_1", "virgin");
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(2));

            //Quickfilter of a string
            selenium.QuickFilter(1, 6, 2);
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(1));

            //Delete filter
            selenium.DeleteFilter(2);
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(2));

            //Filter from the combo with subtokens of a MList
            selenium.FilterSelectToken(0, "label=Album", true);
            selenium.FilterSelectToken(1, "label=Songs", true);
            selenium.FilterSelectToken(2, "value=Count", false);
            selenium.AddFilter(2);
            selenium.FilterSelectOperation(2, "value=GreaterThan");
            selenium.Type("value_2", "1");
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(1));

            //Delete all filters
            selenium.DeleteFilter(2);
            selenium.DeleteFilter(1);
            selenium.DeleteFilter(0);
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(12));

            //QuickFilter from header of a Lite
            selenium.QuickFilterFromHeader(5, 0); //Label
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(0));
            OpenFinderAndSelectFirstOrderedById(selenium, "value_0_");
            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("{0}:visible".Formato(LinesTestExtensions.LineFindSelector("value_0_"))));
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(2));
            selenium.DeleteFilter(0);

            //Top
            selenium.SetElementsPerPageToFinder("5");
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(5));
            Assert.IsTrue(selenium.IsElementPresent("jq=.sf-pagination-left:contains('5')"));
            selenium.SetElementsPerPageToFinder("-1"); 
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(12));
            Assert.IsTrue(selenium.IsElementPresent("jq=.sf-pagination-left:contains('12')"));
        }

        [TestMethod]
        public void SearchControl002_FiltersInPopup()
        {
            CheckLoginAndOpen(ViewRoute("Band", null));

            //open search popup
            selenium.LineFind("Members_", 0);

            string prefix = "Members_0_"; //prefix for all the popup

            selenium.Search(prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(8, prefix));

            //Quickfilter of a bool
            selenium.QuickFilter(1, 5, 0, prefix);

            //Quickfilter of an enum
            selenium.QuickFilter(1, 6, 1, prefix);

            selenium.Search(prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(7, prefix));

            //Quickfilter of an int
            selenium.QuickFilter(4, 3, 2, prefix);
            selenium.FilterSelectOperation(2, "value=GreaterThan", prefix);
            selenium.Search(prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(3, prefix));

            selenium.DeleteFilter(2, prefix);
            selenium.Search(prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(7, prefix));

            //Filter of a Lite from the combo
            selenium.FilterSelectToken(0, "label=Artist", true, prefix);
            selenium.AddFilter(2, prefix);
            OpenFinderAndSelectFirstOrderedById(selenium, prefix + "value_2_");
            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("{0}:visible".Formato(LinesTestExtensions.LineFindSelector(prefix + "value_2_"))));
            selenium.Search(prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(1, prefix));
            selenium.DeleteFilter(2, prefix);

            //Filter from the combo with Subtokens
            selenium.FilterSelectToken(0, "label=Artist", true, prefix);
            selenium.FilterSelectToken(1, "label=Name", false, prefix);
            selenium.AddFilter(2, prefix);
            selenium.FilterSelectOperation(2, "value=EndsWith", prefix);
            selenium.Type(prefix + "value_2", "a");
            selenium.Search(prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(1, prefix));

            //Delete all filters
            selenium.DeleteFilter(2, prefix);
            selenium.DeleteFilter(1, prefix);
            selenium.DeleteFilter(0, prefix);
            selenium.Search(prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(8, prefix));

            //QuickFilter from header of an int
            selenium.QuickFilterFromHeader(3, 0, prefix);
            selenium.FilterSelectOperation(0, "value=LessThanOrEqual", prefix);
            selenium.Type(prefix + "value_0", "2");
            selenium.Search(prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(2, prefix));
            selenium.DeleteFilter(0, prefix);

            //Top
            selenium.SetElementsPerPageToFinder("5", prefix);
            selenium.Search(prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(5, prefix));
            selenium.SetElementsPerPageToFinder("-1", prefix); 
            selenium.Search(prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(8, prefix));
        }

        private void OpenFinderAndSelectFirstOrderedById(ISelenium selenium, string prefix)
        {
            selenium.LineFind(prefix);
            selenium.Sort(3, true, prefix);
            selenium.Search(prefix);
            selenium.SelectRowCheckbox(0, prefix);
            selenium.PopupOk(prefix);
        }

        [TestMethod]
        public void SearchControl003_Orders()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            int authorCol = 4;

            //Ascending
            selenium.Sort(authorCol, true);
            selenium.WaitAjaxFinished(() => selenium.IsEntityInRow(1, "Album;5"));
            
            int labelCol = 5;
            
            //Multiple orders
            selenium.SortMultiple(labelCol, true);
            selenium.WaitAjaxFinished(() => selenium.IsEntityInRow(1, "Album;7"));
            selenium.TableHeaderMarkedAsSorted(authorCol, true, true);
            
            //Multiple orders: change column order type to descending
            selenium.SortMultiple(labelCol, false);
            selenium.WaitAjaxFinished(() => selenium.IsEntityInRow(1, "Album;5"));
            selenium.TableHeaderMarkedAsSorted(authorCol, true, true);

            //Cancel multiple clicking a new order without Shift
            selenium.Sort(labelCol, true);
            selenium.WaitAjaxFinished(() => selenium.IsEntityInRow(1, "Album;12"));
            selenium.TableHeaderMarkedAsSorted(authorCol, true, false);
        }

        [TestMethod]
        public void SearchControl004_OrdersInPopup()
        {
            CheckLoginAndOpen(ViewRoute("Band", null));

            //open search popup
            selenium.LineFind("Members_", 0);
            
            string prefix = "Members_0_"; //prefix for all the popup

            int isMaleCol = 5;

            //Ascending
            selenium.Sort(isMaleCol, true, prefix);
            selenium.WaitAjaxFinished(() => selenium.IsElementInCell(1, isMaleCol, "input:checkbox[value=false]", prefix));
            
            //Descending
            selenium.Sort(isMaleCol, false, prefix);
            selenium.WaitAjaxFinished(() => selenium.IsElementInCell(1, isMaleCol, "input:checkbox[value=true]", prefix));

            int nameCol = 4;

            //Multiple orders
            selenium.SortMultiple(nameCol, true, prefix);
            selenium.WaitAjaxFinished(() => selenium.IsEntityInRow(1, "Artist;1", prefix));
            selenium.TableHeaderMarkedAsSorted(isMaleCol, false, true, prefix);

            //Multiple orders: change column order type to descending
            selenium.SortMultiple(nameCol, false, prefix);
            selenium.WaitAjaxFinished(() => selenium.IsEntityInRow(1, "Artist;8", prefix));
            selenium.TableHeaderMarkedAsSorted(isMaleCol, false, true, prefix);

            int idCol = 3;

            //Cancel multiple clicking a new order without Shift
            selenium.Sort(idCol, true, prefix);
            selenium.WaitAjaxFinished(() => selenium.IsEntityInRow(1, "Artist;1", prefix));
            selenium.TableHeaderMarkedAsSorted(isMaleCol, false, false, prefix);
            selenium.TableHeaderMarkedAsSorted(nameCol, false, false, prefix);
        }

        [TestMethod]
        public void SearchControl005_UserColumns()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            //Add 2 user columns
            selenium.FilterSelectToken(0, "label=Label", true);
            selenium.FilterSelectToken(1, "label=Id", false);
            selenium.AddColumn("Label.Id");

            selenium.FilterSelectToken(1, "label=Name", false);
            selenium.AddColumn("Label.Name");

            //Edit names
            selenium.EditColumnName(8, "Label Id");

            //Search with userColumns
            selenium.Search();
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.CellSelector(selenium, 1, 8)));

            //Move columns
            selenium.MoveColumn(8, "Label Id", true);
            selenium.MoveColumn(7, "Label Id", false);

            //Delete one of the usercolumns
            selenium.RemoveColumn(8, 9);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(0));
            selenium.Search();
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.CellSelector(selenium, 1, 8)));
            Assert.IsTrue(!selenium.IsElementPresent(SearchTestExtensions.CellSelector(selenium, 1, 9)));
        }

        [TestMethod]
        public void SearchControl006_UserColumnsInPopup()
        {
            CheckLoginAndOpen(ViewRoute("Band", null));

            //open search popup
            selenium.LineFind("Members_", 0);

            string prefix = "Members_0_"; //prefix for all the popup

            //User columns are not present in popup
            Assert.IsFalse(selenium.IsElementPresent("jq=#{0}sfSearchControl .sf-add-column".Formato(prefix)));

            //Edit names
            selenium.EditColumnName(5, "Male", prefix);

            //Move columns
            selenium.MoveColumn(3, "Id", false, prefix);
            selenium.MoveColumn(4, "Id", true, prefix);

            //Delete column
            selenium.RemoveColumn(4, 8, prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(0, prefix));
            selenium.Search(prefix);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.CellSelector(selenium, 1, 7, prefix)));
            Assert.IsTrue(!selenium.IsElementPresent(SearchTestExtensions.CellSelector(selenium, 1, 8, prefix)));
        }

        [TestMethod]
        public void SearchControl007_ImplementedByFinder()
        {
            CheckLoginAndOpen(FindRoute("IAuthorDN"));
            
            selenium.Search();

            //Results of implementing types
            Assert.IsTrue(selenium.IsEntityInRow(1, new Lite<ArtistDN>(1).Key()));
            Assert.IsTrue(selenium.IsEntityInRow(9, new Lite<BandDN>(1).Key()));

            //Filters
            selenium.FilterSelectToken(0, "label=Id", false);
            selenium.AddFilter(0);
            selenium.Type("value_0", "1");
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(2)); //Entity with id 1 of each type

            selenium.DeleteFilter(0);
            selenium.FilterSelectToken(0, "value=Entity", true);
            selenium.FilterSelectToken(1, "value=(Artist)", true);
            selenium.FilterSelectToken(2, "Id", false);
            selenium.AddFilter(0);
            selenium.Type("value_0", "1"); 
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(1)); //only ArtistDN;1

            //Create implemented type
            selenium.SearchCreateWithImpl("Artist");
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#Dead")); //there's an artist valueline
        }

        [TestMethod]
        public void SearchControl008_MultiplyFinder()
        {
            CheckLoginAndOpen(FindRoute("Artist"));
            selenium.CheckAddFilterEnabled(false);
            selenium.CheckAddColumnEnabled(false);

            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(8));

            //[Num]
            selenium.FilterSelectToken(0, "label=Artist", true);
            selenium.CheckAddFilterEnabled(true);
            selenium.CheckAddColumnEnabled(true);
            selenium.CheckAddFilterEnabled(true);
            selenium.CheckAddColumnEnabled(true);
            selenium.FilterSelectToken(1, "label=Friends", true);
            selenium.CheckAddFilterEnabled(false);
            selenium.CheckAddColumnEnabled(false);
            selenium.FilterSelectToken(2, "value=Count", false);
            selenium.CheckAddFilterEnabled(true);
            selenium.CheckAddColumnEnabled(true);
                        
            selenium.AddFilter(0);
            selenium.Type("value_0", "1");
            selenium.Search();
            selenium.AssertMultiplyMessage(false);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(3));
            
            selenium.AddColumn("Entity.Friends.Count");
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(3));
            selenium.AssertMultiplyMessage(false);

            selenium.FilterSelectToken(2, "value=", false);
            selenium.CheckAddFilterEnabled(false);
            selenium.CheckAddColumnEnabled(false);

            selenium.DeleteFilter(0);
            selenium.RemoveColumn(9, 9);

            //Element
            selenium.FilterSelectToken(2, "value=Element", true);
            selenium.CheckAddFilterEnabled(true);
            selenium.CheckAddColumnEnabled(true);
            
            selenium.AddColumn("Entity.Friends.Count");
            selenium.Search();
            selenium.AssertMultiplyMessage(true);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(10));

            selenium.AddFilter(0);
            selenium.LineFindAndSelectElements("value_0_", false, new int[] { 2 });
            selenium.Search();
            selenium.AssertMultiplyMessage(true);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(3));

            selenium.DeleteFilter(0);
            selenium.RemoveColumn(9, 9);

            //Any
            selenium.FilterSelectToken(2, "value=Any", true);
            selenium.CheckAddFilterEnabled(true);
            selenium.CheckAddColumnEnabled(false);

            selenium.AddFilter(0);
            selenium.LineFindAndSelectElements("value_0_", false, new int[] { 2 });
            selenium.Search();
            selenium.AssertMultiplyMessage(false);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(3));

            selenium.FilterSelectToken(3, "value=Name", false);
            selenium.AddFilter(1);
            selenium.Type("value_1", "i");
            selenium.Search();
            selenium.AssertMultiplyMessage(false);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(0));
            selenium.Type("value_1", "arcy");
            selenium.Search();
            selenium.AssertMultiplyMessage(false);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(3));

            selenium.DeleteFilter(1);
            selenium.DeleteFilter(0);

            //All
            selenium.FilterSelectToken(2, "value=All", true);
            selenium.CheckAddFilterEnabled(true);
            selenium.CheckAddColumnEnabled(false);

            selenium.AddFilter(0);
            selenium.FilterSelectOperation(0, "value=DistinctTo");
            selenium.LineFindAndSelectElements("value_0_", false, new int[] { 2 });
            selenium.Search();
            selenium.AssertMultiplyMessage(false);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(5));

            selenium.FilterSelectToken(3, "value=Name", false);
            selenium.AddFilter(1);
            selenium.Type("value_1", "Corgan");
            selenium.Search();
            selenium.AssertMultiplyMessage(false);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(4));
        }
    }
}
