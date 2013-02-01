using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Utilities;
using Signum.Entities;
using Signum.Utilities.ExpressionTrees;
using Signum.Test.Environment;

namespace Signum.Test.LinqProvider
{
    [TestClass]
    public class SingleFirstTest
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            Starter.StartAndLoad();
        }

        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [TestMethod]
        public void SelectFirstOrDefault()
        {
            var bandsCount = Database.Query<BandDN>().Select(b => new
            {
                b.Name,
                Members = b.Members.Select(a => new { a.Name, a.Sex }).ToString(p => "{0} ({1})".Formato(p.Name, p.Sex), "\r\n")
            }).ToList();

            var bands1 = Database.Query<BandDN>().Select(b => new { b.Name, Member = b.Members.FirstOrDefault().Name }).ToList();
            var bands2 = Database.Query<BandDN>().Select(b => new { b.Name, Member = b.Members.FirstEx().Name }).ToList();
            var bands3 = Database.Query<BandDN>().Select(b => new { b.Name, Member = b.Members.SingleOrDefaultEx().Name }).ToList();
            var bands4 = Database.Query<BandDN>().Select(b => new { b.Name, Member = b.Members.SingleEx().Name }).ToList();

            var bands1b = Database.Query<BandDN>().Select(b => new { b.Name, Member = b.Members.FirstOrDefault(a => a.Sex == Sex.Female).Name }).ToList();
            var bands2b = Database.Query<BandDN>().Select(b => new { b.Name, Member = b.Members.FirstEx(a => a.Sex == Sex.Female).Name }).ToList();
            var bands3b = Database.Query<BandDN>().Select(b => new { b.Name, Member = b.Members.SingleOrDefaultEx(a => a.Sex == Sex.Female).Name }).ToList();
            var bands4b = Database.Query<BandDN>().Select(b => new { b.Name, Member = b.Members.SingleEx(a => a.Sex == Sex.Female).Name }).ToList();
        }
       

        [TestMethod]
        public void SelectSingleCellWhere()
        {
            var list = Database.Query<BandDN>()
                .Where(b => b.Members.OrderBy(a => a.Sex).Select(a => a.Sex).FirstEx() == Sex.Male)
                .Select(a => a.Name)
                .ToList();
        }

        [TestMethod]
        public void SelectSingleCellSingle()
        {
            var list = Database.Query<BandDN>().Select(b => new
            {
                FirstName = b.Members.Select(m => m.Name).FirstEx(),
                FirstOrDefaultName = b.Members.Select(m => m.Name).FirstOrDefault(),
                SingleName = b.Members.Select(m => m.Name).SingleEx(),
                SingleOrDefaultName = b.Members.Select(m => m.Name).SingleOrDefaultEx(),
            }).ToList();
        }

        [TestMethod]
        public void SelectDoubleSingle()
        {
            var query = Database.Query<BandDN>().Select(b => new
            {
                b.Members.FirstEx().Name,
                b.Members.FirstEx().Dead,
                b.Members.FirstEx().Sex,
            });

            query.ToList();

            Assert.AreEqual(1, query.QueryText().CountRepetitions("APPLY"));
        }

        [TestMethod]
        public void SelecteNestedFirstOrDefault()
        {
            var neasted = ((from b in Database.Query<BandDN>()
                            select b.Members.Select(a => a.Sex).FirstOrDefault())).ToList();
        }


        [TestMethod]
        public void SelecteNestedFirstOrDefaultNullify()
        {
            var neasted = ((from b in Database.Query<BandDN>()
                            select b.Members.Where(a => a.Name.StartsWith("a")).Select(a => (Sex?)a.Sex).FirstOrDefault())).ToList();
        }

        [TestMethod]
        public void SelectGroupLast()
        {
            var result = (from lab in Database.Query<LabelDN>()
                          join al in Database.Query<AlbumDN>().DefaultIfEmpty() on lab equals al.Label into g
                          select new
                          {
                              lab.Id,
                              lab.Name,
                              NumExecutions = (int?)g.Count(),
                              LastExecution = (from al2 in Database.Query<AlbumDN>()
                                               where al2.Id == g.Max(a => (int?)a.Id)
                                               select al2.ToLite()).FirstOrDefault()
                          }).ToList();
        }
    }
}
