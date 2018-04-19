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
    public class LiteCastTest : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LiteCastAnalyzer();
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void CastToEntity()
        {
            TestDiagnostic("Impossible to convert Lite<T> to T. Consider using Entity or Retrieve", @"
Lite<Entity> lite = null;
Entity entity = (Entity)lite;       
            ");
        }

        [TestMethod]
        public void CastToLite()
        {
         
            TestDiagnostic("Impossible to convert T to Lite<T>. Consider using ToLite or ToLiteFat", @"
Entity entity = null;
Lite<Entity> lite = (Lite<Entity>)entity;
            ");
        }

        [TestMethod]
        public void AsToEntity()
        {
            TestDiagnostic("Impossible to convert Lite<T> to T. Consider using Entity or Retrieve", @"
Lite<Entity> lite = null;
Entity entity = lite as Entity;       
            ");
        }

        [TestMethod]
        public void AsToLite()
        {

            TestDiagnostic("Impossible to convert T to Lite<T>. Consider using ToLite or ToLiteFat", @"
Entity entity = null;
Lite<Entity> lite = entity as Lite<Entity>;
            ");
        }

        [TestMethod]
        public void IsToEntity()
        {
            TestDiagnostic("Impossible to convert Lite<T> to T. Consider using Entity or Retrieve", @"
Lite<Entity> lite = null;
var result = lite is Entity;       
            ");
        }

        [TestMethod]
        public void IsToLite()
        {

            TestDiagnostic("Impossible to convert T to Lite<T>. Consider using ToLite or ToLiteFat", @"
Entity entity = null;
var result = entity is Lite<Entity>;
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
                    Id = LiteCastAnalyzer.DiagnosticId,
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