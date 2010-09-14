using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Entities;
using Signum.Engine.Basics;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Diagnostics;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine;
using Signum.Utilities.DataStructures;

namespace Signum.Test.Extensions
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class EntityGroupMemoryTransformerTest
    {
        class ExpressionAsserter : ExpressionComparer
        {
            public ExpressionAsserter()
                : base(new ScopedDictionary<ParameterExpression, ParameterExpression>(null))
            {
            }

            public static new void AreEqual(Expression a, Expression b)
            {
                new ExpressionAsserter().Compare(a, b);
            }

            protected override bool Compare(Expression a, Expression b)
            {
                bool result = base.Compare(a, b);
                if (!result)
                    Assert.Fail("Different expressions: \r\n{0}\r\n{1}".Formato(a.NiceToString(), b.NiceToString()));
                return result; 
            }
        }

        public EntityGroupMemoryTransformerTest()
        {

        }

        void AssertTrans(Expression<Func<PersonDN, bool>> expr, Expression<Func<PersonDN, bool>> expectedResult)
        {
            Expression result = EntityGroupLogic.MemoryTransformer.ToMemory(expr);

            ExpressionAsserter.AreEqual(result, expectedResult); 
        }

        string Rojo = "";

        [TestMethod]
        public void SimpleSR()
        {
            AssertTrans(p => p.Coche.SmartRetrieve().Color == Rojo,
                        p => p.Coche.EntityOrNull == null ?
                             p.Coche.InDB().Any(c => c.Color == Rojo) :
                             p.Coche.Entity.Color == Rojo);
        }


        [TestMethod]
        public void SimpleSRWithOtherCondition()
        {
            bool IsWeekend = true;

            AssertTrans(p => p.Coche.SmartRetrieve().Color == Rojo && IsWeekend,
                        p => (p.Coche.EntityOrNull == null ? p.Coche.InDB().Any(c => c.Color == Rojo) :
                              p.Coche.Entity.Color == Rojo) && IsWeekend);
        }

        [TestMethod]
        public void DoubleSRChain()
        {
            AssertTrans(p => p.Coche.SmartRetrieve().Marca.SmartRetrieve().Nombre == "M",
                        p => p.Coche.EntityOrNull == null ? p.Coche.InDB().Any(c => c.Marca.Entity.Nombre == "M") :
                             p.Coche.Entity.Marca.EntityOrNull == null ? p.Coche.Entity.Marca.InDB().Any(m => m.Nombre == "M") :
                             p.Coche.Entity.Marca.Entity.Nombre == "M");
        }

        [TestMethod]
        public void SimpleSrJoinSimpleSr()
        {
            PersonDN Yo = null; ;

            AssertTrans(p => p.Coche.SmartRetrieve().Marca == Yo.Coche.SmartRetrieve().Marca,
                p => p.Coche.EntityOrNull == null ?
                            (Yo.Coche.EntityOrNull == null ? p.Coche.InDB().Any(c => Yo.Coche.InDB().Any(c2 => c.Marca == c2.Marca)) :
                                                             p.Coche.InDB().Any(c => c.Marca == Yo.Coche.Entity.Marca)) :
                            (Yo.Coche.EntityOrNull == null ? Yo.Coche.InDB().Any(c => p.Coche.Entity.Marca == c.Marca) :
                                                             p.Coche.Entity.Marca == Yo.Coche.Entity.Marca)); 
        }

        [TestMethod]
        public void DoubleSRChainDatabase()
        {
            AssertTrans(p => Database.Query<MarcaDN>().Contains(p.Coche.SmartRetrieve().Marca.SmartRetrieve()),
                        p => p.Coche.EntityOrNull == null ? p.Coche.InDB().Any(c => Database.Query<MarcaDN>().Contains(c.Marca.Entity)) :
                             p.Coche.Entity.Marca.EntityOrNull == null ? p.Coche.Entity.Marca.InDB().Any(m => Database.Query<MarcaDN>().Contains(m)) :
                             Database.Query<MarcaDN>().Contains(p.Coche.Entity.Marca.Entity));
        }

        [TestMethod]
        public void DoubleSRChainDatabaseLambda()
        {
            AssertTrans(p => Database.Query<MarcaDN>().Any(m => m == p.Coche.SmartRetrieve().Marca.SmartRetrieve()),
                        p => Database.Query<MarcaDN>().Any(m =>
                                p.Coche.EntityOrNull == null ? p.Coche.InDB().Any(c => m == c.Marca.Entity) :
                                p.Coche.Entity.Marca.EntityOrNull == null ? p.Coche.Entity.Marca.InDB().Any(m2 => m == m2) :
                                m == p.Coche.Entity.Marca.Entity));
        }

        [TestMethod]
        public void SimpleSmartTypeIs()
        {
            AssertTrans(p => p.Coche.SmartTypeIs<CocheDN>(),
                        p => p.Coche.RuntimeType == typeof(CocheDN));
        }


        [TestMethod]
        public void SimpleSRSmartTypeIs()
        {
            AssertTrans(p => p.Coche.SmartRetrieve().Marca.SmartTypeIs<MarcaDN>() ,
                        p => p.Coche.EntityOrNull == null ? p.Coche.InDB().Any(c => c.Marca.Entity is MarcaDN) :
                             p.Coche.Entity.Marca.RuntimeType == typeof(MarcaDN));
        }

    }

    [Serializable]
    public class PersonDN : IdentifiableEntity
    {
        Lite<CocheDN> coche;
        public Lite<CocheDN> Coche
        {
            get { return coche; }
            set { Set(ref coche, value, () => Coche); }
        }
    }

    [Serializable]
    public class CocheDN : Entity
    {
        Lite<MarcaDN> marca;
        public Lite<MarcaDN> Marca
        {
            get { return marca; }
            set { Set(ref marca, value, () => Marca); }
        }

        string color;
        public string Color
        {
            get { return color; }
            set { Set(ref color, value, () => Color); }
        }

    }

    [Serializable]
    public class MarcaDN : Entity
    {
        string nombre;
        public string Nombre
        {
            get { return nombre; }
            set { Set(ref nombre, value, () => Nombre); }
        }
    }


}
