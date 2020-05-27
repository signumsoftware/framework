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
    public class ExpressionFieldTest : DiagnosticVerifier
    {

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ExpressionFieldAnalyzer();
        }

        [TestMethod]
        public void ExpressionExplicitNotFound()
        {
            TestDiagnostic("field 'MyExpression' not found", @"
        static Expression<Func<Entity, int>> MyExpressionBad;        
        [ExpressionField(""MyExpression"")]
        public static int OperationLogs(this Entity e) => 0;", assertErrors: false);
        }

        [TestMethod]
        public void ExpressionExplicitWrongType()
        {
            TestDiagnostic("type of 'MyExpression' should be 'Expression<Func<Entity, int>>' instead of 'Expression<Func<Entity, long, int>>'", @"
        static Expression<Func<Entity, long, int>> MyExpression;        
        [ExpressionField(""MyExpression"")]
        public static int OperationLogs(this Entity e) => 0;");
        }

        [TestMethod]
        public void ExpressionExplicitCorrect()
        {
            TestDiagnostic(null, @"
        static Expression<Func<Entity, int>> MyExpression;        
        [ExpressionField(""MyExpression"")]
        public static int OperationLogs(this Entity e) => 0;");
        }

        [TestMethod]
        public void ExpressionExplicitNotCorrectNullable()
        {
            TestDiagnostic("type of 'MyExpression' should be 'Expression<Func<Entity, string?>>' instead of 'Expression<Func<Entity, string>>'", @"
        static Expression<Func<Entity, string>> MyExpression;        
        [ExpressionField(""MyExpression"")]
        public static string? OperationLogs(this Entity e) => """";");
        }

        [TestMethod]
        public void ExpressionExplicitCorrectNullable()
        {
            TestDiagnostic(null, @"
        static Expression<Func<Entity, string?>> MyExpression;        
        [ExpressionField(""MyExpression"")]
        public static string? OperationLogs(this Entity e) => """";");
        }


        private void TestDiagnostic(string expectedError, string code, bool staticClass = true, bool assertErrors = true)
        {
            string test = Surround(code, staticClass);
            if (expectedError == null)
                VerifyCSharpDiagnostic(test, assertErrors, new DiagnosticResult[0]);
            else
                VerifyCSharpDiagnostic(test, assertErrors, new DiagnosticResult
                {
                    Id = ExpressionFieldAnalyzer.DiagnosticId,
                    Message = string.Format("'OperationLogs' should reference an static field of type Expression<T> with the same signature ({0})", expectedError),
                    Severity = DiagnosticSeverity.Warning,
                });
        }

        private static string Surround(string member, bool staticClass = true)
        {
            return $@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;

namespace ConsoleApplication1
{{
    {(staticClass ? "static" : null)} class Bla
    {{
{member}
    }}
}}";

        }
    }
}
