using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Entities;

namespace Signum.Test
{
    [TestClass]
    public class PrimaryKeyTest
    {
        [TestMethod]
        public void CartesianProduct()
        {

            PrimaryKey id = 3;

            PrimaryKey id2 = 3L;

            if (id == id2)
            {

            }


          
        }
    }
}
