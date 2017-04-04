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
            ResetLazy<string> val = new ResetLazy<string>(() => "hola" + i++);

            val.Reset(); //reset before initialized
            var str1 = val.Value;
            val.Reset(); //reset after initialized

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
        public void LevenshteinDistanceWeight()
        {
            Func<StringDistance.Choice<char>, int> w = c => c.HasAdded && char.IsNumber(c.Added) || c.HasRemoved && char.IsNumber(c.Removed) ? 10 : 1;

            Assert.AreEqual(10, d.LevenshteinDistance("hola", "ho5la", weight: w));
            Assert.AreEqual(10, d.LevenshteinDistance("ho5la", "hola", weight: w));
            Assert.AreEqual(10, d.LevenshteinDistance("ho5la", "hojla", weight: w));
            Assert.AreEqual(10, d.LevenshteinDistance("hojla", "ho5la", weight: w));
            Assert.AreEqual(10, d.LevenshteinDistance("ho5la", "ho6la", weight: w));
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

        [TestMethod]
        public void Diff()
        {
            var result = d.Diff("en un lugar de la mancha".ToCharArray(), "in un place de la mincha".ToCharArray());

            var str = result.ToString("");

            Assert.AreEqual("-e+in un +pl-u-ga-r+c+e de la m-a+incha", str); 
        }

        [TestMethod]
        public void DiffWords()
        {
            var result = d.DiffWords(
                "Soft drinks, coffees, teas, beers, and ginger ales", 
                "Soft drinks, coffees, teas and beers");

            var str = result.ToString("");

            Assert.AreEqual("Soft drinks, coffees, teas-,- -beers-, and -ginger- -ales+beers", str);

        }

        [TestMethod]
        public void Choices()
        {
            var result = d.LevenshteinChoices("en un lugar de la mancha".ToCharArray(), "in un legarito de la mincha".ToCharArray());

            var str = result.ToString("");

            Assert.AreEqual("[-e+i]n un l[-u+e]gar+i+t+o de la m[-a+i]ncha", str);
        }

        [TestMethod]
        public void DiffText()
        {
            var text1 = 
@"  Hola Pedro
Que tal
Bien

Adios Juan";

            var text2 =
@"  Hola Pedri

Que til
Adios Juani";

            var result = d.DiffText(text1, text2);

            var str = result.ToString(l => (l.Action == StringDistance.DiffAction.Added ? "[+]" :
                l.Action == StringDistance.DiffAction.Removed ? "[-]" : "[=]") + l.Value.ToString(""), "\n");

            Assert.AreEqual(
@"[=]  Hola -Pedro+Pedri
[-]-Que tal
[-]-Bien
[=]
[+]+Que til
[=]Adios -Juan+Juani", str);

        }
    }
}
