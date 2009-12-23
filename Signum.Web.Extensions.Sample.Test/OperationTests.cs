using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WatiN.Core;
using WatiN.Core.UnitTests;
using Signum.Web;

namespace Signum.Web.Extensions.Sample.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class OperationTests
    {
        public OperationTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void operation_001_ConstructFromLite()
        {
            using (var ie = new IE("http://localhost/Signum.Web.Extensions.Sample/View/Album/1"))
                constructFromLite(ie);
            
            using (var ff = new FireFox("http://localhost/Signum.Web.Extensions.Sample/View/Album/1"))
                constructFromLite(ff);
        }

        private static void constructFromLite(Browser b)
        {
            //Click clone
            Div cloneOp = b.Div(d => d.ClassName == "OperationDiv" && d.InnerHtml == "Clone");
            cloneOp.Click();

            string prefix = "_New";
            
            //Check album clone is created in popup
            Assert.IsTrue(b.Div(prefix + "Temp").Exists);
            
            //Close popup
            b.Div(prefix + ViewDataKeys.BtnCancel).Click();

            //Popup is removed completely from DOM
            Assert.IsTrue(!b.Div(prefix + "Temp").Exists);

            //Retry
            cloneOp.Click();


        }
    }
}
