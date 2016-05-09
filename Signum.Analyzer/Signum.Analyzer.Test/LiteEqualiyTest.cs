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
        public void ExpressionFieldNoReturnType()
        {
            TestDiagnostic("no return type", @"
Lite<Entity> lite = null;
Entity entity = null;
var condition = lite == entity;       
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
                    Message = string.Format("You should not compare Lite<T> and T directly", expectedError),
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