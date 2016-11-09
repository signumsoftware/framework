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
        }");
        }

        [TestMethod]
        public void ExpressionNoReturnExpression()
        {
            TestDiagnostic("no return expression", @"        
        [ExpressionField]
        public static int OperationLogs(this Entity e)
        {
            return;
        }");
        }

        [TestMethod]
        public void ExpressionNoGetter()
        {
            TestDiagnostic("no getter", @"        
        [ExpressionField]
        public static int OperationLogs { set; }");
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
        public static int OperationLogs => 1");
        }

        [TestMethod]
        public void ExpressionGetterExpressionBody()
        {
            TestDiagnostic("no invocation", @"        
        [ExpressionField]
        public static int OperationLogs(this Entity e) => 1");
        }

        [TestMethod]
        public void ExpressionNoEvaluate()
        {
            TestDiagnostic("no Evaluate", @"
        static Expression<Func<Entity, int>> MyExpression;         
        [ExpressionField]
        public static int OperationLogs(this Entity e) => MyExpression.Invoke(e)");
        }

        [TestMethod]
        public void ExpressionNoStaticField()
        {
            TestDiagnostic("no static field", @"
        static Expression<Func<Entity, int>> MyExpression { get; }         
        [ExpressionField]
        public static int OperationLogs(this Entity e) => MyExpression.Evaluate(e)");
        }

        [TestMethod]
        public void ExpressionThis()
        {
            TestDiagnostic("first argument should be 'this'", @"
        static Expression<Func<Entity, int>> MyExpression;        
        [ExpressionField]
        public int OperationLogs(this Entity e) => MyExpression.Evaluate(e)");
        }

        [TestMethod]
        public void ExpressionMissingArgument()
        {
            TestDiagnostic("missing argument 'e'", @"
        static Expression<Func<Bla, int>> MyExpression;        
        [ExpressionField]
        public int OperationLogs(this Entity e) => MyExpression.Evaluate(this)");
        }

        [TestMethod]
        public void ExpressionExtra()
        {
            TestDiagnostic("extra parameters", @"
        static Expression<Func<Bla, Entity, Entity, int>> MyExpression;        
        [ExpressionField]
        public int OperationLogs(this Entity e) => MyExpression.Evaluate(this, e, e)");
        }

        [TestMethod]
        public void ExpressionCorrect()
        {
            TestDiagnostic(null, @"
        static Expression<Func<Bla, Entity, int>> MyExpression;        
        [ExpressionField]
        public int OperationLogs(this Entity e) => MyExpression.Evaluate(this, e)");
        }

        [TestMethod]
        public void ExpressionExplicitNotFound()
        {
            TestDiagnostic("field 'MyExpression' not found", @"
        static Expression<Func<Bla, Entity, int>> MyExpressionBad;        
        [ExpressionField(""MyExpression"")]
        public int OperationLogs(this Entity e) => 0");
        }

        [TestMethod]
        public void ExpressionExplicitWrongType()
        {
            TestDiagnostic("type of 'MyExpression' should be 'Expression<Func<Bla, Entity, int>>'", @"
        static Expression<Func<Entity, int>> MyExpression;        
        [ExpressionField(""MyExpression"")]
        public int OperationLogs(this Entity e) => 0", withIncludes: true);
        }

        [TestMethod]
        public void ExpressionExplicitCorrect()
        {
            TestDiagnostic(null, @"
        static Expression<Func<Bla, Entity, int>> MyExpression;        
        [ExpressionField(""MyExpression"")]
        public int OperationLogs(this Entity e) => 0", withIncludes: true);
        }


        private void TestDiagnostic(string expectedError, string code, bool withIncludes = false)
        {
            string test = Surround(code, withIncludes: withIncludes);
            if (expectedError == null)
                VerifyDiagnostic(test, new DiagnosticResult[0]);
            else
                VerifyDiagnostic(test, new DiagnosticResult
                {
                    Id = ExpressionFieldAnalyzer.DiagnosticId,
                    Message = string.Format("'OperationLogs' should reference an static field of type Expression<T> with the same signature ({0})", expectedError),
                    Severity = DiagnosticSeverity.Warning,
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
using System.Linq.Expressions;" : null) +  @"

namespace ConsoleApplication1
{
    class Bla
    {"
+ expression +
@"
    }
}";
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
        public string GetId => GetIdExpression.Evaluate(this);");
        }

        [TestMethod]
        public void ExpressionFixInstancePropertyImplicitThis()
        {
            TestCodeFix(@"
        string id;

        [ExpressionField]
        public string GetId => id;",
        @"
        string id;

        static Expression<Func<Bla, string>> GetIdExpression = @this => @this.id;
        [ExpressionField]
        public string GetId => GetIdExpression.Evaluate(this);");
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
        public string GetId { get { return GetIdExpression.Evaluate(this); } }");
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

        private void TestCodeFix(string initial, string final)
        {
            VerifyFix(Surround(initial), Surround(final, withIncludes:true));
        }
    }
}