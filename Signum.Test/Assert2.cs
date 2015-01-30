using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Expressions;
using System.IO;

namespace Signum.Test
{
    public static class Assert2
    {
        //for MSTest
        public static void Throws<E>(Action action)
            where E : Exception
        {
            try
            {
                action();
            }
            catch (E)
            {
                return;
            }

            throw new AssertFailedException("No {0} has been thrown".FormatWith(typeof(E).Name));
        }

        public static void Throws<E>(string messageToContain, Action action)
           where E : Exception
        {
            try
            {
                action();
            }
            catch (E ex)
            {
                if(!ex.Message.Contains(messageToContain))
                    throw new AssertFailedException("No {0} has been thrown with message {0}".FormatWith(typeof(E).Name, ex.Message));

                return;
            }

            throw new AssertFailedException("No {0} has been thrown".FormatWith(typeof(E).Name));
        }

        public static void Throws<E>(Func<E, bool> exceptionCondition, Action action)
          where E : Exception
        {
            try
            {
                action();
            }
            catch (E ex)
            {
                if (!exceptionCondition(ex))
                    throw new AssertFailedException("No {0} has been thrown that satisfies the condition".FormatWith(typeof(E).Name));

                return;
            }

            throw new AssertFailedException("No {0} has been thrown".FormatWith(typeof(E).Name));
        }

        public static void AssertAll<T>(this IEnumerable<T> collection, Expression<Func<T, bool>> predicate)
        {
            var func = predicate.Compile();

            foreach (var item in collection)
            {
                if (!func(item))
                    Assert.Fail("'{0}' fails on '{1}'".FormatWith(item, predicate.ToString())); 
            }
        }

        public static void AssertContains<T>(this IEnumerable<T> collection, params T[] elements)
        {
            var hs = collection.ToHashSet();

            string notFound = elements.Where(a => !hs.Contains(a)).CommaAnd();

            if (notFound.HasText())
                Assert.Fail("{0} not found".FormatWith(notFound)); 
        }

        public static void AssertNotContains<T>(this IEnumerable<T> collection, params T[] elements)
        {
            var hs = collection.ToHashSet();

            string found = elements.Where(a => hs.Contains(a)).CommaAnd();

            if (found.HasText())
                Assert.Fail("{0}  found".FormatWith(found));
        }

        public static void AssertExactly<T>(this IEnumerable<T> collection, params T[] elements)
        {
            var hs = collection.ToHashSet();

            string notFound = elements.Where(a => !hs.Contains(a)).CommaAnd();
            string exceeded = hs.Where(a => !elements.Contains(a)).CommaAnd(); ;

            if (notFound.HasText() && exceeded.HasText())
                Assert.Fail("{0} not found and {1} exceeded".FormatWith(notFound, exceeded));

            if(notFound.HasText())
                Assert.Fail("{0} not found".FormatWith(notFound));

            if (exceeded.HasText())
                Assert.Fail("{0} exceeded".FormatWith(exceeded));

        }

        public static new bool Equals(object obj, object obj2)
        {
            throw new NotSupportedException("Use Assert.AreEquals instead");
        }
    }
}
