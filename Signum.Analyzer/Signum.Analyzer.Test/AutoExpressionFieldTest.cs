using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using Signum.Analyzer;

namespace Signum.Analyzer.Test;

[TestClass]
public class AutoExpressionFieldTest : CodeFixVerifier
{
    protected override CodeFixProvider GetCSharpCodeFixProvider()
    {
        return new AutoExpressionFieldFixProvider();
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new AutoExpressionFieldAnalyzer();
    }

    //Diagnostic and CodeFix both triggered and checked for
    [TestMethod]
    public void AutoExpressionFieldNoReturnType()
    {
        TestDiagnostic("no return type", @"        
    [AutoExpressionField]
    public static void OperationLogs(this Entity e)
    {
    }");
    }

    [TestMethod]
    public void AutoExpressionComplexParameter()
    {
        TestDiagnostic("complex parameter 'e'", @"        
    [AutoExpressionField]
    public static int OperationLogs(ref Entity e)
    {
        return 0;
    }");
    }

    [TestMethod]
    public void AutoExpression2Statements()
    {
        TestDiagnostic("2 statements", @"        
    [AutoExpressionField]
    public static int OperationLogs(this Entity e)
    {
        var a = 2;
        return 0;
    }");
    }

    [TestMethod]
    public void AutoExpressionNoReturn()
    {
        TestDiagnostic("no return", @"        
    [AutoExpressionField]
    public static int OperationLogs(this Entity e)
    {
        var a = 2;
    }", assertErrors: false);
    }

    [TestMethod]
    public void AutoExpressionNoReturnExpression()
    {
        TestDiagnostic("no return expression", @"        
    [AutoExpressionField]
    public static int OperationLogs(this Entity e)
    {
        return;
    }", assertErrors: false);
    }

    [TestMethod]
    public void AutoExpressionNoGetter()
    {
        TestDiagnostic("no getter", @"        
    [AutoExpressionField]
    public static int OperationLogs { set; }", assertErrors: false);
    }

    [TestMethod]
    public void AutoExpressionNoGetterBody()
    {
        TestDiagnostic("no getter body", @"        
    [AutoExpressionField]
    public static int OperationLogs { get; }");
    }

    [TestMethod]
    public void AutoExpressionMethodExpressionBody()
    {
        TestDiagnostic("no As.Expression", @"        
    [AutoExpressionField]
    public static int OperationLogs => 1;");
    }

    [TestMethod]
    public void AutoExpressionGetterExpressionBody()
    {
        TestDiagnostic("no As.Expression", @"        
    [AutoExpressionField]
    public static int OperationLogs(this Entity e) => 1;");
    }

    [TestMethod]
    public void AutoExpressionWrongMethod()
    {
        TestDiagnostic("no As.Expression", @"
    [AutoExpressionField]
    public static int OperationLogs(this Entity e) => Math.Max(1, 2);");
    }

    [TestMethod]
    public void AutoExpressionNoLambda()
    {
        TestDiagnostic("the call to As.Expression should have a lambda as argument", @"
    [AutoExpressionField]
    public static int OperationLogs(this Entity e) => As.Expression<int>(null);");
    }

    [TestMethod]
    public void AutoExpressionImplicitCasting()
    {
        TestDiagnostic("the call to As.Expression returns 'PrimaryKey' but is implicitly converted to 'int?'", @"
    [AutoExpressionField]
    public static int? OperationLogs(Entity e) => As.Expression(() => e.Id);", assertErrors: false);
    }

    [TestMethod]
    public void AutoExpressionCorrectWithError()
    {
        TestDiagnostic(null, @"
    [AutoExpressionField]
    public static int OperationLogs(Entity e) => As.Expression(() => (int)e.NotThere);", assertErrors: false);
    }

    [TestMethod]
    public void AutoExpressionCorrect()
    {
        TestDiagnostic(null, @"
    [AutoExpressionField]
    public static int OperationLogs(Entity e) => As.Expression(() => (int)e.Id);");
    }


    private void TestDiagnostic(string expectedError, string code, bool staticClass = true, bool assertErrors = true)
    {
        string test = Surround(code, staticClass);
        if (expectedError == null)
            VerifyCSharpDiagnostic(test, assertErrors, new DiagnosticResult[0]);
        else
            VerifyCSharpDiagnostic(test, assertErrors, new DiagnosticResult
            {
                Id = AutoExpressionFieldAnalyzer.DiagnosticId,
                Message = string.Format("'OperationLogs' should call As.Expression(() => ...) ({0})", expectedError),
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

namespace ConsoleApplication1
{{
{(staticClass ? "static" : null)} class Bla
{{
{member}
}}
}}";
    }

    [TestMethod]
    public void AutoExpressionFixStatic()
    {
        TestCodeFix(@"
    [AutoExpressionField]
    public static int GetId(Entity e) => (int)e.Id;",
    @"
    [AutoExpressionField]
    public static int GetId(Entity e) => As.Expression(() => (int)e.Id);");
    }

    [TestMethod]
    public void AutoExpressionFixInstanceProperty()
    {
        TestCodeFix(@"
    [AutoExpressionField]
    public string IdToStr => this.ToString();",
    @"
    [AutoExpressionField]
    public string IdToStr => As.Expression(() => this.ToString());", staticClass: false);
    }

    [TestMethod]
    public void AutoExpressionFixInstancePropertyStatement()
    {
        TestCodeFix(@"
    [AutoExpressionField]
    public string IdToStr { get { return ToString(); } }",
    @"
    [AutoExpressionField]
    public string IdToStr { get { return As.Expression(() => ToString()); } }", staticClass: false);
    }


    [TestMethod]
    public void AutoExpressionFixInstanceMethodStatement()
    {
        TestCodeFix(@"
    [AutoExpressionField]
    public string GetId() { return ToString(); }",
    @"
    [AutoExpressionField]
    public string GetId() { return As.Expression(() => ToString()); }", staticClass: false);
    }

    [TestMethod]
    public void AutoExpressionFixInstanceMethodStatementTrivia()
    {
        TestCodeFix(@"
    [AutoExpressionField]
    public string GetId() =>
        ToString();",
    @"
    [AutoExpressionField]
    public string GetId() =>
        As.Expression(() => ToString());", staticClass: false);
    }

    [TestMethod]
    public void AutoExpressionExplicitCast()
    {
        TestCodeFix(@"
    [AutoExpressionField]
    public int? GetId() => As.Expression(()=>3);",
    @"
    [AutoExpressionField]
    public int? GetId() => As.Expression(()=> (int?)3);", staticClass: false);
    }

    private void TestCodeFix(string initial, string final, bool staticClass = true)
    {
        VerifyCSharpFix(
            Surround(initial, staticClass: staticClass),
            Surround(final, staticClass: staticClass),
        assertNoInitialErrors: false);
    }
}
