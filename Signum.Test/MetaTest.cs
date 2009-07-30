using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using System.Reflection;
using Signum.Engine.Linq;
using Signum.Entities;
using Signum.Entities.DynamicQuery;

namespace Signum.Test
{
    [TestClass]
    public class MetaTest
    {
        [TestMethod]
        public void NoMetadata()
        {
            Assert.IsNull(DynamicQuery.QueryMetadata(Database.Query<NoteDN>().Select(a => a.Entity)));
        }

        [TestMethod]
        public void RawEntity()
        {
            var dic = DynamicQuery.QueryMetadata(Database.Query<NoteDN>());
            Assert.AreEqual(dic.Count, typeof(NoteDN).GetProperties(BindingFlags.Instance | BindingFlags.Public).Length);

            Assert.IsTrue(dic.Values.All(m => m is CleanMeta));
        }

        [TestMethod]
        public void AnonymousType()
        {
            var dic = DynamicQuery.QueryMetadata(Database.Query<NoteDN>().Select(a => new { a.Entity, a.Error, a.ToStr.Length, Sum = a.ToStr + a.ToStr }));
            Assert.IsInstanceOfType(dic["Entity"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Error"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Length"], typeof(DirtyMeta));
            Assert.IsInstanceOfType(dic["Sum"], typeof(DirtyMeta));
        }

        public class Bla
        {
            public string ToStr { get; set; }
            public int Length { get; set; }
        }

        [TestMethod]
        public void NamedType()
        {
            var dic = DynamicQuery.QueryMetadata(Database.Query<NoteDN>().Select(a => new Bla { ToStr = a.ToStr, Length = a.ToStr.Length }));
            Assert.IsInstanceOfType(dic["ToStr"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Length"], typeof(DirtyMeta));
        }



        [TestMethod]
        public void ComplexJoin()
        {
            var dic = DynamicQuery.QueryMetadata(
                    from a in Database.Query<ProductDN>()
                    join b in Database.Query<ProductLineDN>() on a equals b.Product
                    select new { a.Name, b.Num, Sum = a.Name.Length + b.Num });

            Assert.IsInstanceOfType(dic["Name"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Num"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Sum"], typeof(DirtyMeta));

            Assert.AreEqual(((DirtyMeta)dic["Sum"]).Properties.Select(cm => cm.Member.Name).Order().ToString(","), "Name,Num"); 
        }

        [TestMethod]
        public void ComplexJoinGroup()
        {
            var dic = DynamicQuery.QueryMetadata(
                    from a in Database.Query<ProductDN>()
                    join b in Database.Query<ProductLineDN>() on a equals b.Product into g
                    select new { a.Name, Num = g.Count()});

            Assert.IsInstanceOfType(dic["Name"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Num"], typeof(DirtyMeta));

            Assert.IsTrue(((DirtyMeta)dic["Num"]).Properties.Count == 0);
        }

        [TestMethod]
        public void ComplexGroup()
        {
            var dic = DynamicQuery.QueryMetadata(
                    from pl in Database.Query<ProductLineDN>()
                    group pl by pl.Product into g
                    select new { g.Key, Num = g.Count() });

            Assert.IsInstanceOfType(dic["Key"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Num"], typeof(DirtyMeta));

            Assert.IsTrue(((DirtyMeta)dic["Num"]).Properties.Count == 0);
        }

        [TestMethod]
        public void SelectMany()
        {
            var dic = DynamicQuery.QueryMetadata(
                    from p in Database.Query<ProductDN>()
                    from t in p.Taxes
                    select new { p.Name, t.TaxName, t.Val}
                    );

            Assert.IsInstanceOfType(dic["Name"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["TaxName"], typeof(CleanMeta));
            Assert.IsInstanceOfType(dic["Val"], typeof(CleanMeta));

            Assert.IsNotNull(((CleanMeta)dic["TaxName"]).Parent);
            Assert.IsNotNull(((CleanMeta)dic["TaxName"]).Parent.Parent);

            Assert.IsNotNull(((CleanMeta)dic["Val"]).Parent);
            Assert.IsNotNull(((CleanMeta)dic["Val"]).Parent.Parent);
        }
    }

    [Serializable]
    public class ProductDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, "Name"); }
        }

        MList<TaxDN> taxes;
        public MList<TaxDN> Taxes
        {
            get { return taxes; }
            set { Set(ref taxes, value, "Taxes"); }
        }

        public override string ToString()
        {
            return name;
        }
    }

    [Serializable]
    public class ProductLineDN : Entity
    {
        ProductDN product;
        public ProductDN Product
        {
            get { return product; }
            set { Set(ref product, value, "Product"); }
        }

        int num;
        public int Num
        {
            get { return num; }
            set { Set(ref num, value, "Num"); }
        }
    }

    [Serializable]
    public class TaxDN : EmbeddedEntity
    {
        int val;
        public int Val
        {
            get { return val; }
            set { Set(ref val, value, "Val"); }
        }

        string taxName;
        public string TaxName
        {
            get { return taxName; }
            set { Set(ref taxName, value, "TaxName"); }
        }
    }
}
