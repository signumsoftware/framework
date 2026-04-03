using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using Signum.Analyzer;

namespace Signum.Analyzer.Test;

[TestClass]
public class LiteEqualityTest : CodeFixVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new LiteEqualityAnalyzer();
    }

    protected override CodeFixProvider GetCSharpCodeFixProvider()
    {
        return new LiteEqualityCodeFixProvider();
    }

    [TestMethod]
    public void CompareTwoLites()
    {
        TestDiagnostic("SF0031", "Avoid comparing two Lite<T> by reference, consider using 'Is' extension method", @"
Lite<OrangeEntity> o1 = null;
Lite<OrangeEntity> o2 = null;
var condition = o1 == o2;     
        ");
    }


    [TestMethod]
    public void CompareTwoEntities()
    {
        TestDiagnostic("SF0032", "Avoid comparing two Entities by reference, consider using 'Is' extension method", @"
OrangeEntity o1 = null;
OrangeEntity o2 = null;
var condition = o1 == o2;     
        ");
    }


    [TestMethod]
    public void CompareLiteAndEntity()
    {
        TestDiagnostic("SF0033", "Impossible to compare Lite<T> and T, consider using 'Is' extension method", @"
Lite<Entity> lite = null;
Entity entity = null;
var condition = lite == entity;       
        ");
    }

    [TestMethod]
    public void CompareIncompatibleTypes()
    {
        TestDiagnostic("SF0034","Impossible to compare Lite<AppleEntity> and Lite<OrangeEntity>", @"
Lite<AppleEntity> apple = null;
Lite<OrangeEntity> orange = null;
var condition = apple == orange;       
        ");
    }

    [TestMethod]
    public void CompareIncompatibleAbstractTypes()
    {
        TestDiagnostic("SF0034", "Impossible to compare Lite<AbstractBananaEntity> and Lite<OrangeEntity>", @"
Lite<AbstractBananaEntity> banana = null;
Lite<OrangeEntity> orange = null;
var condition = banana == orange;       
        ");
    }

    [TestMethod]
    public void CompareBaseType()
    {

        TestDiagnostic("SF0031", "Avoid comparing two Lite<T> by reference, consider using 'Is' extension method", @"
Lite<Entity> type = null;
Lite<OrangeEntity> query = null;
var condition = type == query;       
        ");
    }


    [TestMethod]
    public void CompareDifferentInterfaces()
    {
        TestDiagnostic("SF0031", "Avoid comparing two Lite<T> by reference, consider using 'Is' extension method", @"
Lite<ISpider> type = null;
Lite<IMan> baseLite = null;
var condition = type == baseLite;  //Could be SpiderMan!     
        ");
    }

    [TestMethod]
    public void CompareDifferentInterfaceEntity()
    {
        TestDiagnostic("SF0031", "Avoid comparing two Lite<T> by reference, consider using 'Is' extension method", @"
Lite<ISpider> type = null;
Lite<OrangeEntity> baseLite = null;
var condition = type == baseLite;  //Could be SpiderMan!     
        ");
    }

    private void TestDiagnostic(string id, string expectedError, string code,  bool withIncludes = false, bool assertErrors = true)
    {
        string test = Surround(code, withIncludes: withIncludes);
        if (expectedError == null)
            VerifyCSharpDiagnostic(test, assertErrors, new DiagnosticResult[0]);
        else
            VerifyCSharpDiagnostic(test, assertErrors, new DiagnosticResult
            {
                Id = id,
                Message = expectedError,
                Severity = id is "SF0031" or "SF0032" ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error,
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

public abstract class AbstractBananaEntity : Entity {}

class Bla
{  
    void Foo() 
    {"

+ expression +
@"      }
}
}";
    }

    private void TestCodeFix(string initial, string final, bool allowNewCompilerDiagnostics = false)
    {
        VerifyCSharpFix(
            Surround(initial),
            Surround(final),
        assertNoInitialErrors: false,
        allowNewCompilerDiagnostics: allowNewCompilerDiagnostics);
    }


    [TestMethod]
    public void FixLiteEquals()
    {
        TestCodeFix(
@"
Lite<AppleEntity> ap1 = null;
Lite<AppleEntity> ap2 = null;
var condition = ap1 == ap2;",
@"
Lite<AppleEntity> ap1 = null;
Lite<AppleEntity> ap2 = null;
var condition = ap1.Is(ap2);");
    }

    [TestMethod]
    public void FixEntityEquals()
    {
        TestCodeFix(
@"
AppleEntity ap1 = null;
AppleEntity ap2 = null;
var condition = ap1 == ap2;",
@"
AppleEntity ap1 = null;
AppleEntity ap2 = null;
var condition = ap1.Is(ap2);");
    }

    [TestMethod]
    public void FixLiteEntityEquals()
    {
        TestCodeFix(
@"
Lite<AppleEntity> ap1 = null;
AppleEntity ap2 = null;
var condition = ap1 == ap2;",
@"
Lite<AppleEntity> ap1 = null;
AppleEntity ap2 = null;
var condition = ap1.Is(ap2);");
    }

    [TestMethod]
    public void FixLiteEqualsError()
    {
        TestCodeFix(
@"
Lite<OrangeEntity> or1 = null;
Lite<AppleEntity> ap2 = null;
var condition = or1 == ap2;",
@"
Lite<OrangeEntity> or1 = null;
Lite<AppleEntity> ap2 = null;
var condition = or1.Is(ap2);", allowNewCompilerDiagnostics: true);
    }


    [TestMethod]
    public void FixEntityEqualsError()
    {
        TestCodeFix(
@"
OrangeEntity or1 = null;
AppleEntity ap2 = null;
var condition = or1 == ap2;",
@"
OrangeEntity or1 = null;
AppleEntity ap2 = null;
var condition = or1.Is(ap2);", allowNewCompilerDiagnostics: true);
    }

    [TestMethod]
    public void FixLiteEntityEqualsError()
    {
        TestCodeFix(
@"
Lite<OrangeEntity> or1 = null;
AppleEntity ap2 = null;
var condition = or1 == ap2;",
@"
Lite<OrangeEntity> or1 = null;
AppleEntity ap2 = null;
var condition = or1.Is(ap2);", allowNewCompilerDiagnostics: true);
    }

  
}
