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
    public class ExpressionFieldTest : CodeFixVerifier
    {
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ExpressionFieldFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ExpressionFieldAnalyzer();
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void ExpressionFieldNoReturnType()
        {
            TestDiagnostic("no return type", @"        
        [ExpressionField]
        public static void OperationLogs(this Entity e)
        {
        }");
        }

        [TestMethod]
        public void ExpressionComplexParameter()
        {
            TestDiagnostic("complex parameter 'e'", @"        
        [ExpressionField]
        public static int OperationLogs(ref Entity e)
        {
            return 0;
        }");
        }

        [TestMethod]
        public void Expression2Statements()
        {
            TestDiagnostic("2 statements", @"        
        [ExpressionField]
        public static int OperationLogs(this Entity e)
        {
            var a = 2;
            return 0;
        }");
        }

        [TestMethod]
        public void ExpressionNoReturn()
        {
            TestDiagnostic("no return", @"        
        [ExpressionField]
        public static int OperationLogs(this Entity e)
        {
            var a = 2;
        }", assertErrors: false);
        }

        [TestMethod]
        public void ExpressionNoReturnExpression()
        {
            TestDiagnostic("no return expression", @"        
        [ExpressionField]
        public static int OperationLogs(this Entity e)
        {
            return;
        }", assertErrors: false);
        }

        [TestMethod]
        public void ExpressionNoGetter()
        {
            TestDiagnostic("no getter", @"        
        [ExpressionField]
        public static int OperationLogs { set; }", assertErrors: false);
        }

        [TestMethod]
        public void ExpressionNoGetterBody()
        {
            TestDiagnostic("no getter body", @"        
        [ExpressionField]
        public static int OperationLogs { get; }");
        }

        [TestMethod]
        public void ExpressionMethodExpressionBody()
        {
            TestDiagnostic("no invocation", @"        
        [ExpressionField]
        public static int OperationLogs => 1;");
        }

        [TestMethod]
        public void ExpressionGetterExpressionBody()
        {
            TestDiagnostic("no invocation", @"        
        [ExpressionField]
        public static int OperationLogs(this Entity e) => 1;");
        }

        [TestMethod]
        public void ExpressionNoEvaluate()
        {
            TestDiagnostic("no Evaluate", @"
        static Expression<Func<Entity, int>> MyExpression;         
        [ExpressionField]
        public static int OperationLogs(this Entity e) => MyExpression.Invoke(e)", assertErrors: false);
        }

        [TestMethod]
        public void ExpressionNoStaticField()
        {
            TestDiagnostic("no static field", @"
        static Expression<Func<Entity, int>> MyExpression { get; }         
        [ExpressionField]
        public static int OperationLogs(this Entity e) => MyExpression.Evaluate(e)", assertErrors: false);
        }

        [TestMethod]
        public void ExpressionThis()
        {
            TestDiagnostic("first argument should be 'this'", @"
        static Expression<Func<Entity, int>> MyExpression;        
        [ExpressionField]
        public int OperationLogs(this Entity e) => MyExpression.Evaluate(e)", assertErrors: false);
        }

        [TestMethod]
        public void ExpressionMissingArgument()
        {
            TestDiagnostic("missing argument 'e'", @"
        static Expression<Func<int>> MyExpression;        
        [ExpressionField]
        public static int OperationLogs(this Entity e) => MyExpression.Evaluate();", includeExpression: true);
        }

        [TestMethod]
        public void ExpressionExtra()
        {
            TestDiagnostic("extra parameters", @"
        static Expression<Func<Entity, Entity, int>> MyExpression;        
        [ExpressionField]
        public static int OperationLogs(this Entity e) => MyExpression.Evaluate(e, e);", assertErrors: false);
        }

        [TestMethod]
        public void ExpressionCorrect()
        {
            TestDiagnostic(null, @"
        static Expression<Func<Bla, Entity, int>> MyExpression;        
        [ExpressionField]
        public int OperationLogs(Entity e) => MyExpression.Evaluate(this, e);", staticClass: false, includeExpression: true);
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
            TestDiagnostic("type of 'MyExpression' should be 'Expression<Func<Entity, int>>'", @"
        static Expression<Func<Entity, long, int>> MyExpression;        
        [ExpressionField(""MyExpression"")]
        public static int OperationLogs(this Entity e) => 0;", includeExpression: true);
        }

        [TestMethod]
        public void ExpressionExplicitCorrect()
        {
            TestDiagnostic(null, @"
        static Expression<Func<Entity, int>> MyExpression;        
        [ExpressionField(""MyExpression"")]
        public static int OperationLogs(this Entity e) => 0;", includeExpression: true);
        }

        [TestMethod]
        public void ExpressionExplicitCorrectNullable()
        {
            TestDiagnostic(null, @"
        static Expression<Func<Entity, string?>> MyExpression;        
        [ExpressionField(""MyExpression"")]
        public static string? OperationLogs(this Entity e) => """";", includeExpression: true);
        }


        private void TestDiagnostic(string expectedError, string code, bool includeExpression = false, bool staticClass = true, bool assertErrors = true)
        {
            string test = Surround(code, includeExpression, staticClass);
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

        private static string Surround(string member, bool includeExpression = false, bool staticClass = true)
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
{(includeExpression ? @"using System.Linq.Expressions;
" : null)}

namespace ConsoleApplication1
{{
    {(staticClass ? "static" : null)} class Bla
    {{
{member}
    }}
}}";
        }

        [TestMethod]
        public void ExpressionFixStatic()
        {
            TestCodeFix(@"
        [ExpressionField]
        public static int GetId(Entity e) => (int)e.Id;",
        @"
        static Expression<Func<Entity, int>> GetIdExpression = e => (int)e.Id;
        [ExpressionField]
        public static int GetId(Entity e) => GetIdExpression.Evaluate(e);");
        }

        [TestMethod]
        public void ExpressionFixInstanceProperty()
        {
            TestCodeFix(@"

        [ExpressionField]
        public string GetId => this.ToString();",
        @"

        static Expression<Func<Bla, string>> GetIdExpression = @this => @this.ToString();
        [ExpressionField]
        public string GetId => GetIdExpression.Evaluate(this);", staticClass: false);
        }

        [TestMethod]
        public void ExpressionFixInstancePropertyImplicitThis()
        {
            TestCodeFix(@"
        string? id;

        [ExpressionField]
        public string? GetId => id;",
        @"
        string? id;

        static Expression<Func<Bla, string? >> GetIdExpression = @this => @this.id;
        [ExpressionField]
        public string? GetId => GetIdExpression.Evaluate(this);", staticClass: false);
        }

        [TestMethod]
        public void ExpressionFixInstancePropertyStatementImplicitThis()
        {
            TestCodeFix(@"

        [ExpressionField]
        public string GetId { get { return ToString(); } }",
        @"

        static Expression<Func<Bla, string>> GetIdExpression = @this => @this.ToString();
        [ExpressionField]
        public string GetId { get { return GetIdExpression.Evaluate(this); } }", staticClass: false);
        }
        

        [TestMethod]
        public void ExpressionFixDiagnosticStatic2()
        {
            TestCodeFix(@"
        static Expression<Func<Entity, int>> GetIdExpression = e => (int)e.Id + 2;

        [ExpressionField]
        public static int GetId(Entity e) => (int)e.Id;",
        @"
        static Expression<Func<Entity, int>> GetIdExpression = e => (int)e.Id + 2;

        static Expression<Func<Entity, int>> GetIdExpression2 = e => (int)e.Id;
        [ExpressionField]
        public static int GetId(Entity e) => GetIdExpression2.Evaluate(e);");
        }

        private void TestCodeFix(string initial, string final, bool staticClass = true)
        {
            VerifyCSharpFix(
                Surround(initial, includeExpression: false, staticClass: staticClass),
                Surround(final, includeExpression: true, staticClass: staticClass),
            assertNoInitialErrors: false);
        }
    }
}
