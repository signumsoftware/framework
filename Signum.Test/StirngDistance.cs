using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Utilities;

namespace Signum.Test
{
    [TestClass]
    public class StringDistanceTest
    {
        StringDistance d = new StringDistance();

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
