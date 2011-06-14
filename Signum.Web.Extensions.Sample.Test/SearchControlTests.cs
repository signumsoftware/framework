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
            Common.Start(testContext);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Common.MyTestCleanup();
        }

        [TestMethod]
        public void Filters()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            //No filters
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(12));

            //Quickfilter of a Lite
            selenium.QuickFilter(1, 3, 0);
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(4));

            //Filter of a Lite from the combo
            selenium.FilterSelectToken(0, "label=Label", true);
            selenium.AddFilter(1);
            selenium.LineFindAndSelectElements("value_1_", false, new int[]{0});
            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("{0}:visible".Formato(LinesTestExtensions.LineFindSelector("value_1_"))));
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(2));
            selenium.DeleteFilter(1);

            //Filter from the combo with Subtokens
            selenium.FilterSelectToken(0, "label=Label", true);
            selenium.ExpandTokens(1);
            selenium.FilterSelectToken(1, "label=Name", false);
            selenium.AddFilter(1);
            selenium.Type("value_1", "virgin");
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(2));

            //Quickfilter of a string
            selenium.QuickFilter(1, 5, 2);
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(1));

            //Delete filter
            selenium.DeleteFilter(2);
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(2));

            //Filter from the combo with subtokens of a MList
            selenium.FilterSelectToken(0, "label=Album", true);
            selenium.ExpandTokens(1);
            selenium.FilterSelectToken(1, "label=Songs", true);
            selenium.ExpandTokens(2);
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
            selenium.QuickFilterFromHeader(4, 0); //Label
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(0));
            selenium.LineFindAndSelectElements("value_0_", false, new int[] { 0 });
            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("{0}:visible".Formato(LinesTestExtensions.LineFindSelector("value_0_"))));
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(2));
            selenium.DeleteFilter(0);

            //Top
            selenium.SetTopToFinder("5");
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(5));
            Assert.IsTrue(selenium.IsElementPresent("jq=#rowsFoundCount:contains(5)"));
            Assert.IsTrue(selenium.IsElementPresent("jq=.rows-found-count-maximum"));
            selenium.SetTopToFinder(""); 
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(12));
            Assert.IsTrue(selenium.IsElementPresent("jq=#rowsFoundCount:contains(12)"));
            Assert.IsFalse(selenium.IsElementPresent("jq=.rows-found-count-maximum"));
        }

        [TestMethod]
        public void FiltersInPopup()
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
            selenium.LineFindAndSelectElements(prefix + "value_2_", false, new int[] { 0 });
            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("{0}:visible".Formato(LinesTestExtensions.LineFindSelector(prefix + "value_2_"))));
            selenium.Search(prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(1, prefix));
            selenium.DeleteFilter(2, prefix);

            //Filter from the combo with Subtokens
            selenium.FilterSelectToken(0, "label=Artist", true, prefix);
            selenium.ExpandTokens(1, prefix);
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
            selenium.SetTopToFinder("5", prefix);
            selenium.Search(prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(5, prefix));
            selenium.SetTopToFinder("", prefix); 
            selenium.Search(prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(8, prefix));
        }

        [TestMethod]
        public void Orders()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            int authorCol = 3;

            //Ascending
            selenium.Sort(authorCol, true);
            selenium.WaitAjaxFinished(() => selenium.IsEntityInRow(1, "Album;5"));
            
            int labelCol = 4;
            
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
        public void OrdersInPopup()
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
        public void UserColumns()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            //Add 2 user columns
            selenium.FilterSelectToken(0, "label=Label", true);
            selenium.ExpandTokens(1);
            selenium.FilterSelectToken(1, "label=Id", false);
            selenium.AddColumn("Label.Id");

            selenium.FilterSelectToken(1, "label=Name", false);
            selenium.AddColumn("Label.Name");

            //Edit names
            selenium.EditColumnName(7, "Label Id");

            //Search with userColumns
            selenium.Search();
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.CellSelector(1, 8)));

            //Move columns
            Assert.IsFalse(selenium.CanMoveColumn(1, true));
            Assert.IsFalse(selenium.CanMoveColumn(8, false));
            selenium.MoveColumn(7, "Label Id", true);
            selenium.MoveColumn(6, "Label Id", false);

            //Delete one of the usercolumns
            selenium.RemoveColumn(7, 8);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(0));
            selenium.Search();
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.CellSelector(1, 7)));
            Assert.IsTrue(!selenium.IsElementPresent(SearchTestExtensions.CellSelector(1, 8)));
        }

        [TestMethod]
        public void UserColumnsInPopup()
        {
            CheckLoginAndOpen(ViewRoute("Band", null));

            //open search popup
            selenium.LineFind("Members_", 0);

            string prefix = "Members_0_"; //prefix for all the popup

            //User columns are not present in popup
            Assert.IsFalse(selenium.IsElementPresent("jq=#{0}divFilters .sf-add-column".Formato(prefix)));

            //Edit names
            selenium.EditColumnName(5, "Male", prefix);

            //Move columns
            Assert.IsFalse(selenium.CanMoveColumn(3, true, prefix));
            Assert.IsFalse(selenium.CanMoveColumn(8, false, prefix));
            selenium.MoveColumn(3, "Id", false, prefix);
            selenium.MoveColumn(4, "Id", true, prefix);

            //Delete column
            selenium.RemoveColumn(4, 8, prefix);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(0, prefix));
            selenium.Search(prefix);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.CellSelector(1, 7, prefix)));
            Assert.IsTrue(!selenium.IsElementPresent(SearchTestExtensions.CellSelector(1, 8, prefix)));
        }

        [TestMethod]
        public void ImplementedByFinder()
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
            selenium.FilterSelectToken(0, "label=IAuthor", true);
            selenium.ExpandTokens(1);
            selenium.FilterSelectToken(1, "value=({0})".Formato(typeof(ArtistDN).FullName.Replace(".", ":")), true);
            selenium.ExpandTokens(2);
            selenium.FilterSelectToken(2, "Id", false);
            selenium.AddFilter(0);
            selenium.Type("value_0", "1"); 
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(1)); //only ArtistDN;1

            //Create implemented type
            selenium.SearchCreateWithImpl("Artist");
            Assert.IsTrue(selenium.IsElementPresent("jq=#Dead")); //there's an artist valueline
        }

        [TestMethod]
        public void MultiplyFinder()
        {
            CheckLoginAndOpen(FindRoute("Artist"));

            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(8));

            //[Num]
            selenium.FilterSelectToken(0, "label=Artist", true);
            selenium.CheckAddFilterEnabled(true);
            selenium.CheckAddColumnEnabled(true);
            selenium.ExpandTokens(1);
            selenium.CheckAddFilterEnabled(false);
            selenium.CheckAddColumnEnabled(false);
            selenium.FilterSelectToken(1, "label=Friends", true);
            selenium.CheckAddFilterEnabled(false);
            selenium.CheckAddColumnEnabled(false);
            selenium.ExpandTokens(2);
            selenium.FilterSelectToken(2, "value=Count", false);
            selenium.CheckAddFilterEnabled(true);
            selenium.CheckAddColumnEnabled(true);
            
            selenium.AddFilter(0);
            selenium.Type("value_0", "1");
            selenium.Search();
            selenium.CheckMultiplyMessage(false);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(3));
            
            selenium.AddColumn("Entity.Friends.Count");
            selenium.Search();
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(3));
            selenium.CheckMultiplyMessage(false);

            selenium.DeleteFilter(0);
            selenium.RemoveColumn(8, 8);

            //Element
            selenium.FilterSelectToken(2, "value=Element", true);
            selenium.CheckAddFilterEnabled(true);
            selenium.CheckAddColumnEnabled(true);
            
            selenium.AddColumn("Entity.Friends.Count");
            selenium.Search();
            selenium.CheckMultiplyMessage(true);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(10));

            selenium.AddFilter(0);
            selenium.LineFindAndSelectElements("value_0_", false, new int[] { 2 });
            selenium.Search();
            selenium.CheckMultiplyMessage(true);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(3));

            selenium.DeleteFilter(0);
            selenium.RemoveColumn(8, 8);

            //Any
            selenium.FilterSelectToken(2, "value=Any", true);
            selenium.CheckAddFilterEnabled(true);
            selenium.CheckAddColumnEnabled(false);

            selenium.AddFilter(0);
            selenium.LineFindAndSelectElements("value_0_", false, new int[] { 2 });
            selenium.Search();
            selenium.CheckMultiplyMessage(false);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(3));

            selenium.ExpandTokens(3);
            selenium.FilterSelectToken(3, "value=Name", true);
            selenium.Type("value_1_", "i");
            selenium.Search();
            selenium.CheckMultiplyMessage(false);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(0));
            selenium.Type("value_1_", "arcy");
            selenium.Search();
            selenium.CheckMultiplyMessage(false);
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
            selenium.CheckMultiplyMessage(false);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(5));

            selenium.ExpandTokens(3);
            selenium.FilterSelectToken(3, "value=Name", true);
            selenium.Type("value_1_", "Corgan");
            selenium.Search();
            selenium.CheckMultiplyMessage(false);
            selenium.WaitAjaxFinished(selenium.ThereAreNRows(4));
        }

        [TestMethod]
        public void EntityCtxMenu_OpExecute()
        {
            CheckLoginAndOpen(FindRoute("Artist"));

            //Search
            selenium.Search();
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.RowSelector(1)));

            string row1col1 = SearchTestExtensions.CellSelector(1, 1);
            
            //ArtistOperations.AssignPersonalAward
            selenium.EntityContextMenu(1);
            selenium.EntityContextMenuClick(1, 1);

            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("{0} > a.sf-entity-ctxmenu-success".Formato(row1col1)));
            Assert.IsFalse(selenium.IsElementPresent("{0} .sf-search-ctxmenu .sf-search-ctxmenu-overlay".Formato(row1col1)));

            //For Michael Jackson there are no operations enabled
            selenium.EntityContextMenu(5);
            //There's not a menu with hrefs => only some text saying there are no operations
            Assert.IsFalse(selenium.IsElementPresent("{0} a".Formato(SearchTestExtensions.EntityContextMenuSelector(5))));
        }

        [TestMethod]
        public void EntityCtxMenu_OpConstructFrom_OpenPopup()
        {
            CheckLoginAndOpen(FindRoute("Band"));

            //Search
            selenium.Search();
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.RowSelector(1)));
            
            string row1col1 = SearchTestExtensions.CellSelector(1, 1);
            
            //Band.CreateFromBand
            selenium.EntityContextMenu(1);
            selenium.EntityContextMenuClick(1, 1);

            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector("New_")));

            selenium.Type("New_Name", "ctxtest");
            selenium.Type("New_Year", DateTime.Now.Year.ToString());
            
            selenium.LineFindAndSelectElements("New_Label_", false, new int[]{0});

            selenium.Click("jq=#{0}btnOk".Formato("New_")); //Dont't call PopupOk helper => it makes an ajaxWait and then waitPageLoad fails
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void EntityCtxMenu_OpConstructFrom_Navigate()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            //Search
            selenium.Search();
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.RowSelector(1)));

            //Album.Clone
            selenium.EntityContextMenu(1);
            selenium.EntityContextMenuClick(1, 1);

            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#AlbumOperation_Save"));

            selenium.Type("Name", "ctxtest2");
            selenium.Type("Year", DateTime.Now.Year.ToString());

            selenium.Click("AlbumOperation_Save");
            selenium.WaitForPageToLoad(PageLoadTimeout);
            selenium.MainEntityHasId();
        }

        [TestMethod]
        public void EntityCtxMenu_OpDelete()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            //Album.Delete
            //Order by Id descending so we delete the last cloned album
            int idCol = 2;
            selenium.Sort(idCol, true);
            selenium.Sort(idCol, false);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SearchTestExtensions.RowSelector(1)));

            Assert.IsTrue(selenium.IsElementPresent(SearchTestExtensions.RowSelector(14)));
            Assert.IsFalse(selenium.IsElementPresent(SearchTestExtensions.RowSelector(15)));

            string row1col1 = SearchTestExtensions.CellSelector(1, 1);

            selenium.EntityContextMenu(1);
            selenium.EntityContextMenuClick(1, 2);

            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
            selenium.WaitForPageToLoad(PageLoadTimeout);

            Assert.IsTrue(selenium.IsElementPresent(SearchTestExtensions.RowSelector(13)));
            Assert.IsFalse(selenium.IsElementPresent(SearchTestExtensions.RowSelector(14)));
        }
    }
}
