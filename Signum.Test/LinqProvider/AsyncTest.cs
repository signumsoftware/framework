using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Test.Environment;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for SelectManyTest
    /// </summary>
    public class AsyncTest
    {
        public AsyncTest()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [Fact]
        public void ToListAsync()
        {
            var artistsInBands = Database.Query<BandEntity>().ToListAsync().Result;
        }

        [Fact]
        public void ToArrayAsync()
        {
            var artistsInBands = Database.Query<BandEntity>().ToArrayAsync().Result;
        }

        [Fact]
        public void AverageAsync()
        {
            var artistsInBands = Database.Query<BandEntity>().AverageAsync(a=>a.Members.Count).Result;
        }


        [Fact]
        public void MinAsync()
        {
            var artistsInBands = Database.Query<BandEntity>().MinAsync(a => a.Members.Count).Result;
        }
    }
}
