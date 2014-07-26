using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities;
using System.Diagnostics;
using System.IO;
using Signum.Engine.Linq;
using Signum.Utilities;
using System.Linq.Expressions;
using System.Data.SqlTypes;
using System.Reflection;
using Signum.Test.Environment;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class SelectNestedTest
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            MusicStarter.StartAndLoad();
        }

        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [TestMethod]
        public void SelecteNested()
        {
            var neasted = (from l in Database.Query<LabelDN>()
                           select (from a in Database.Query<AlbumDN>()
                                   where a.Label == l
                                   select  a.ToLite()).ToList()).ToList();
        }

        [TestMethod]
        public void SelecteNestedIB()
        {
            var neasted = (from b in Database.Query<BandDN>()
                           select (from a in Database.Query<AlbumDN>()
                                   where a.Author == b
                                   select a.ToLite()).ToList()).ToList();
        }

        [TestMethod]
        public void SelecteNullableLookupColumns()
        {
            var neasted = (from l in Database.Query<LabelDN>()
                           join o in Database.Query<LabelDN>().DefaultIfEmpty() on l.Owner.Entity equals o
                           group l.ToLite() by o.ToLite() into g
                           select new
                           {
                               Owner = g.Key,
                               List = g.ToList(),
                               Count = g.Count()
                           }).ToList(); 
                          
        }

        [TestMethod]
        public void SelecteGroupBy()
        {
            var neasted = (from l in Database.Query<LabelDN>()
                           group l.ToLite() by l.Owner into g
                           select new
                           {
                               Owner = g.Key,
                               List = g.ToList(),
                               Count = g.Count()
                           }).ToList();

        }

        [TestMethod]
        public void SelecteNestedIBPlus()
        {
            var neasted = (from l in Database.Query<LabelDN>()
                           select (from a in Database.Query<AlbumDN>()
                                   where a.Label == l
                                   select new { Label = l.ToLite(), Author = a.Author.ToLite(), Album = a.ToLite() }).ToList()).ToList();
        }

        [TestMethod]
        public void SelecteNestedNonKey()
        {
            var neasted = (from a in Database.Query<AlbumDN>()
                           select new
                               {
                                   Alumum = a.ToLite(),
                                   Friends = (from b in Database.Query<AlbumDN>()
                                              where a.Label == b.Label
                                              select b.ToLite()).ToList()
                               }).ToList();
        }

        [TestMethod]
        public void SelecteNestedContanins()
        {
            var neasted = (from a in Database.Query<ArtistDN>()
                           select (from b in Database.Query<BandDN>()
                                   where b.Members.Contains(a)
                                   select b.ToLite()).ToList()).ToList();
        }

        [TestMethod]
        public void SelecteNestedIndePendent1()
        {
            var neasted = (from a in Database.Query<LabelDN>()
                           select (from n in Database.Query<NoteWithDateDN>()
                                   select n.ToLite()).ToList()).ToList();
        }

        [TestMethod]
        public void SelecteNestedIndePendent2()
        {
            var neasted = (from a in Database.Query<LabelDN>()
                           select new
                           {
                               Label = a.ToLite(),
                               Notes = (from n in Database.Query<NoteWithDateDN>()
                                        select n.ToLite()).ToList()
                           }).ToList();
        }

        [TestMethod]
        public void SelecteNestedSemiIndePendent()
        {
            var neasted = (from a in Database.Query<LabelDN>()
                           select (from n in Database.Query<NoteWithDateDN>()
                                   select new
                                   {
                                       Note = n.ToLite(),
                                       Label = a.ToLite(),
                                   }).ToList()).ToList();
        }

        [TestMethod]
        public void SelecteNestedOuterOrder()
        {
            var neasted = (from l in Database.Query<LabelDN>()
                           orderby l.Name
                           select new
                           {
                               Label = l.ToLite(),
                               Notes = (from a in Database.Query<AlbumDN>()
                                        where a.Label == l
                                        select a.ToLite()).ToList()
                           }).ToList();
        }

        [TestMethod]
        public void SelecteNestedOuterOrderTake()
        {
            var neasted = (from l in Database.Query<LabelDN>()
                           orderby l.Name
                           select new
                           {
                               Label = l.ToLite(),
                               Notes = (from a in Database.Query<AlbumDN>()
                                        where a.Label == l
                                        select a.ToLite()).ToList()
                           }).Take(10).ToList();
        }

        [TestMethod]
        public void SelecteNestedInnerOrder()
        {
            var neasted = (from l in Database.Query<LabelDN>()                           
                           select new
                           {
                               Label = l.ToLite(),
                               Notes = (from a in Database.Query<AlbumDN>()
                                        where a.Label == l
                                        orderby a.Name
                                        select a.ToLite()).ToList()
                           }).ToList();
        }

        [TestMethod]
        public void SelecteNestedInnerOrderTake()
        {
            var neasted = (from l in Database.Query<LabelDN>()                           
                           select new
                           {
                               Label = l.ToLite(),
                               Notes = (from a in Database.Query<AlbumDN>()
                                        where a.Label == l
                                        orderby a.Name
                                        select a.ToLite()).Take(10).ToList()
                           }).ToList();
        }

        [TestMethod]
        public void SelecteDoubleNested()
        {
            var neasted = (from l in Database.Query<LabelDN>()
                           select (from a in Database.Query<AlbumDN>()
                                   where a.Label == l
                                   select (from s in a.Songs
                                           select "{0} - {1} - {2}".Formato(l.Name, a.Name, s.Name)).ToList()).ToList()).ToList();
        }

        [TestMethod]
        public void SelecteNestedDoubleOrder()
        {
            var neasted = (from l in Database.Query<LabelDN>()
                           orderby l.Name
                           select (from a in Database.Query<AlbumDN>()
                                   where a.Label == l
                                   orderby a.Name
                                   select a.Name).ToList()).ToList();
        }


        [TestMethod]
        public void SelecteDoubleNestedDoubleOrder()
        {
            var neasted = (from l in Database.Query<LabelDN>()
                           orderby l.Name
                           select (from a in Database.Query<AlbumDN>()
                                   where a.Label == l
                                   orderby a.Name
                                   select (from s in a.Songs
                                           select "{0} - {1} - {2}".Formato(l.Name, a.Name, s.Name)).ToList()).ToList()).ToList();
        }



        [TestMethod]
        public void SelectContainsInt()
        {
            var result = (from b in Database.Query<BandDN>()
                          where b.Members.Select(a => a.Id).Contains(1)
                          select b.ToLite()).ToList();
        }

        [TestMethod]
        public void SelectContainsEnum()
        {
            var result = (from b in Database.Query<BandDN>()
                          where b.Members.Select(a => a.Sex).Contains(Sex.Female)
                          select b.ToLite()).ToList();
        }


        //[TestMethod]
        //public void SelecteNestedAsQueryable()
        //{
        //    var neasted = (from l in Database.Query<LabelDN>()
        //                   select (from a in Database.Query<AlbumDN>()
        //                           where a.Label == l
        //                           select a.ToLite())).ToList();
        //}

        //[TestMethod]
        //public void SelecteNestedAsQueryableAnonymous()
        //{
        //    var neasted = (from l in Database.Query<LabelDN>()
        //                   select new { Elements = (from a in Database.Query<AlbumDN>()
        //                           where a.Label == l
        //                           select a.ToLite())} ).ToList();
        //}

      
    }
}
