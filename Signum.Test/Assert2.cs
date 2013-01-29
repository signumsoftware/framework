using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Test.Properties;
using System.Linq.Expressions;
using System.IO;

namespace Signum.Test
{
    public static class Assert2
    {
        static string solutionDirectory;
        public static string SolutionDirectory
        {
            get
            {
                if (solutionDirectory == null)
                {
                    string directory = Directory.GetCurrentDirectory();

                    while (Path.GetFileName(directory) != "TestResults")
                    {
                        if (Path.GetFileName(directory) == null)
                            throw new InvalidOperationException("TestResults not found");

                        directory = Path.GetDirectoryName(directory);
                    }

                    solutionDirectory = Path.GetDirectoryName(directory);
                }

                return solutionDirectory;
            }
            set { solutionDirectory = value; }
        }

        public static void Throws<T>(Action action)
            where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new AssertFailedException(Resources.No0HasBeenThrown.Formato(typeof(T).Name));
        }

        public static void Throws<T>(Action action, string messageToContain)
           where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if(!ex.Message.Contains(messageToContain))
                    throw new AssertFailedException(Resources.ExceptionThrownDoesNotContainMessage0.Formato(ex.Message));

                return;
            }

            throw new AssertFailedException(Resources.No0HasBeenThrown.Formato(typeof(T).Name));
        }

        public static void AssertAll<T>(this IEnumerable<T> collection, Expression<Func<T, bool>> predicate)
        {
            var func = predicate.Compile();

            foreach (var item in collection)
            {
                if (!func(item))
                    Assert.Fail("'{0}' fails on '{1}'".Formato(item, predicate.NiceToString())); 
            }
        }

        public static void AssertContains<T>(this IEnumerable<T> collection, params T[] elements)
        {
            var hs = collection.ToHashSet();

            string notFound = elements.Where(a => !hs.Contains(a)).CommaAnd();

            if (notFound.HasText())
                Assert.Fail("{0} not found".Formato(notFound)); 
        }

        public static void AssertNotContains<T>(this IEnumerable<T> collection, params T[] elements)
        {
            var hs = collection.ToHashSet();

            string found = elements.Where(a => hs.Contains(a)).CommaAnd();

            if (found.HasText())
                Assert.Fail("{0}  found".Formato(found));
        }

        public static void AssertExactly<T>(this IEnumerable<T> collection, params T[] elements)
        {
            var hs = collection.ToHashSet();

            string notFound = elements.Where(a => !hs.Contains(a)).CommaAnd();
            string exceeded = hs.Where(a => !elements.Contains(a)).CommaAnd(); ;

            if (notFound.HasText() && exceeded.HasText())
                Assert.Fail("{0} not found and {1} exceeded".Formato(notFound, exceeded));

            if(notFound.HasText())
                Assert.Fail("{0} not found".Formato(notFound));

            if (exceeded.HasText())
                Assert.Fail("{0} exceeded".Formato(exceeded));

        }
    }
}
