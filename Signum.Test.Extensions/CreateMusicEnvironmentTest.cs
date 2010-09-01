using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Utilities.Reflection;
using Signum.Engine.Maps;
using Signum.Engine.Authorization;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Signum.Services;
using Signum.Engine.Basics;
using Signum.Utilities;
using Signum.Test.Extensions.Properties;

namespace Signum.Test.Extensions
{
    [TestClass]
    public class CreateMusicEnvironmentTest
    {
        [TestMethod]
        public void CreateMusicEnvironment()
        {
            Starter.StartAndLoad(UserConnections.Replace(Settings.Default.ConnectionString));
        }
    }
}
