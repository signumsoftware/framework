using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine.Processes;
using System.Threading;
using Signum.Utilities;
using Signum.Utilities.DataStructures;

namespace Signum.Test.Extensions
{
    [TestClass]
    public class BlockingQueueTest
    {
        [TestMethod]
        public void BlockingQueue()
        {
            BlockingQueue<int> queue = new BlockingQueue<int>();

            Thread[] producers = 0.To(10).Select(i => new Thread(() => 
            {
                foreach (var j in 0.To(100))
                    queue.Enqueue(j + i * 1000);
                queue.Enqueue(-1);
            })).ToArray();

            ImmutableAVLTree<int,int> tree = ImmutableAVLTree<int,int>.Empty;

            Thread[] consumers = 0.To(10).Select(i => new Thread(() =>
            {
                while (true)
                {
                    int num = queue.Dequeue();
                    if (num == -1)
                        break;
                    Sync.SafeUpdate(ref tree, t => t.Add(num, num));

                }
            })).ToArray();

            foreach (var p in consumers) p.Start();
            foreach (var p in producers) p.Start();

            foreach (var p in producers) p.Join();
            foreach (var p in consumers) p.Join();

            Assert.AreEqual(tree.Keys.Count(), 1000); 
        }
    }
}
