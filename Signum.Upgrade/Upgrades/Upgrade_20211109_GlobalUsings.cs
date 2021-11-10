using Signum.Utilities;
using System.IO;
using System.Linq;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20211109_GlobalUsings : CodeUpgradeBase
{
    public override string Description => "Remove trivial usings and creates GlobalUsing.cs";

    public override void Execute(UpgradeContext uctx)
    {
        var basicUsings = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using Signum.Entities.Reflection;";

        ProcessDirectory(uctx, uctx.EntitiesDirectory, basicUsings + @"
using System.ComponentModel;");

        ProcessDirectory(uctx, uctx.LogicDirectory, basicUsings + @"
using Signum.Entities.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Engine.Basics;");

        ProcessDirectory(uctx, uctx.ReactDirectory, basicUsings + @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Signum.React.ApiControllers;
using Signum.React.Filters;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Web;
using Signum.Engine;
using Signum.Engine.Operations;");

        ProcessDirectory(uctx, uctx.TerminalDirectory, basicUsings + $@"
using Signum.Engine;
using Signum.Engine.Operations;");

        ProcessDirectory(uctx, uctx.TestEnvironmentDirectory, basicUsings + @"
using Signum.Engine;
using Signum.Engine.Operations;
using Xunit;");

        ProcessDirectory(uctx, uctx.TestLogicDirectory, basicUsings + $@"
using Signum.Engine;
using Signum.Engine.Operations;
using Xunit;
using {uctx.ApplicationName}.Test.Environment;");

        ProcessDirectory(uctx, uctx.TestReactDirectory, basicUsings + $@"
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.React.Selenium;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Xunit;
using {uctx.ApplicationName}.Test.Environment;");
    }

    private void ProcessDirectory(UpgradeContext uctx, string directory, string usings)
    {
        var namespaces = usings.Lines().Select(a => a.Trim().After("using ").BeforeLast(";")).ToList();

        uctx.CreateCodeFile(Path.Combine(directory, "Properties", "GlobalUsings.cs"), namespaces.ToString(a => "global using " + a + ";", "\r\n"));

        uctx.ForeachCodeFile(@"*.cs", directory, file =>
        {
            file.RemoveAllLines(a => a.StartsWith("using ") && namespaces.Contains(a.After("using ").Before(";")));
        });
    }
}
