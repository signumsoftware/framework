using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Data;
using Signum.Entities;
using Signum;

namespace Signum.Engine.Test.Personas
{   
    /// <summary>
    ///This is a test class for AADTest and is intended
    ///to contain all AADTest Unit Tests
    ///</summary>
    [TestClass()]
    public class CacheObjetosTest
    {
        /// <summary>
        ///A test for Campo
        ///</summary>
        [TestMethod()]
        public void CachearObjetos()
        {
            using (new EntityCache())
            {
                Persona p = new Persona() { Id = 2 };

                EntityCache.Add(p);
                EntityCache.Add(new Gato() { Id = 3 });

                using (new EntityCache())
                {
                    Assert.IsTrue(EntityCache.Contains<Gato>(3));
                    Assert.IsTrue(EntityCache.Contains<Persona>(2));
                    Assert.AreSame(p, EntityCache.Get<Persona>(2));
                }

                Assert.IsTrue(EntityCache.Contains<Gato>(3));
                Assert.IsTrue(EntityCache.Contains<Persona>(2));
                Assert.AreSame(p, EntityCache.Get<Persona>(2));
            }
        }
    }
}
