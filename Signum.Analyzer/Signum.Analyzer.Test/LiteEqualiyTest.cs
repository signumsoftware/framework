using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using Signum.Analyzer;

namespace Signum.Analyzer.Test
{
    [TestClass]
    public class LiteEqualityTest : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LiteEqualityAnalyzer();
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void CompareLiteAndEntity()
        {
            TestDiagnostic("Impossible to compare Lite<T> and T. Consider using RefersTo method", @"
Lite<Entity> lite = null;
Entity entity = null;
var condition = lite == entity;       
            ");
        }

        [TestMethod]
        public void CompareIncompatibleTypes()
        {
         
            TestDiagnostic("Impossible to compare Lite<AppleEntity> and Lite<OrangeEntity>", @"
Lite<AppleEntity> apple = null;
Lite<OrangeEntity> orange = null;
var condition = apple == orange;       
            ");
        }

        [TestMethod]
        public void CompareIncompatibleAbstractTypes()
        {

            TestDiagnostic("Impossible to compare Lite<AbstractBananaEntity> and Lite<OrangeEntity>", @"
Lite<AbstractBananaEntity> banana = null;
Lite<OrangeEntity> orange = null;
var condition = banana == orange;       
            ");
        }

        [TestMethod]
        public void CompareBaseType()
        {

            TestDiagnostic(null, @"
Lite<Entity> type = null;
Lite<QueryEntity> query = null;
var condition = type == query;       
            ");
        }


        [TestMethod]
        public void CompareDifferentInterfaces()
        {
            TestDiagnostic(null, @"
Lite<ISpider> type = null;
Lite<IMan> baseLite = null;
var condition = type == baseLite;  //Could be SpiderMan!     
            ");
        }

        [TestMethod]
        public void CompareDifferentInterfaceEntity()
        {
            TestDiagnostic(null, @"
Lite<ISpider> type = null;
Lite<OrangeEntity> baseLite = null;
var condition = type == baseLite;  //Could be SpiderMan!     
            ");
        }




        private void TestDiagnostic(string expectedError, string code, bool withIncludes = false)
        {
            string test = Surround(code, withIncludes: withIncludes);
            if (expectedError == null)
                VerifyDiagnostic(test, new DiagnosticResult[0]);
            else
                VerifyDiagnostic(test, new DiagnosticResult
                {
                    Id = LiteEqualityAnalyzer.DiagnosticId,
                    Message = expectedError,
                    Severity = DiagnosticSeverity.Error,
                });
        }

        private static string Surround(string expression, bool withIncludes = false)
        {
            return @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;" + (withIncludes ? @"
using System.Linq.Expressions;" : null) + @"

namespace ConsoleApplication1
{
    public interface ISpider : IEntity {}
    public interface IMan : IEntity {}
    public class AppleEntity : Entity {}
    public class OrangeEntity : Entity {}

    public abstracr class AbstractBananaEntity : Entity {}

    class Bla
    {  
        void Foo() 
        {"

+ expression +
@"      }
    }
}";
        }
    }
}