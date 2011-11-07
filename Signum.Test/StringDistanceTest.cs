using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Utilities;
using System.Diagnostics;
using System.Threading;

namespace Signum.Test
{
    [TestClass]
    public class StringDistanceTest
    {
        StringDistance d = new StringDistance();

        [TestMethod]
        public void ResetLazy()
        {
            int i = 0;
            Lazy<string> val = new Lazy<string>(()=>"hola" + i++, LazyThreadSafetyMode.PublicationOnly);

            val.ResetPublicationOnly(); //reset before initialized
            var str1 = val.Value;
            val.ResetPublicationOnly(); //reset after initialized

            var str2 = val.Value;

            Assert.AreNotEqual(str1, str2); 
        }

        [TestMethod]
        public void LevenshteinDistance()
        {
            Assert.AreEqual(1, d.LevenshteinDistance("hi", "ho"));
            Assert.AreEqual(1, d.LevenshteinDistance("hi", "hil"));
            Assert.AreEqual(1, d.LevenshteinDistance("hi", "h"));

        }

        [TestMethod]
        public void LongestCommonSubsequence()
        {
            Assert.AreEqual(4, d.LongestCommonSubsequence("hallo", "halo"));
            Assert.AreEqual(7, d.LongestCommonSubsequence("SupeMan", "SuperMan"));
            Assert.AreEqual(0, d.LongestCommonSubsequence("aoa", ""));
        }

        [TestMethod]
        public void LongestCommonSubstring()
        {
            Assert.AreEqual(3, d.LongestCommonSubstring("hallo", "halo"));
            Assert.AreEqual(4, d.LongestCommonSubstring("SupeMan", "SuperMan"));
            Assert.AreEqual(0, d.LongestCommonSubstring("aoa", ""));
        }
    }
}
