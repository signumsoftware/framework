using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using Signum.Analyzer;

namespace Signum.Analyzer.Test;

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
        TestDiagnostic("Impossible to convert Lite<T> to T, consider using Entity or Retrieve", @"
Lite<Entity> lite = null;
Entity entity = (Entity)lite;       
        ");
    }

    [TestMethod]
    public void CastToLite()
    {
     
        TestDiagnostic("Impossible to convert T to Lite<T>, consider using ToLite or ToLiteFat", @"
Entity entity = null;
Lite<Entity> lite = (Lite<Entity>)entity;
        ");
    }

    [TestMethod]
    public void AsToEntity()
    {
        TestDiagnostic("Impossible to convert Lite<T> to T, consider using Entity or Retrieve", @"
Lite<Entity> lite = null;
Entity entity = lite as Entity;       
        ");
    }

    [TestMethod]
    public void AsToLite()
    {

        TestDiagnostic("Impossible to convert T to Lite<T>, consider using ToLite or ToLiteFat", @"
Entity entity = null;
Lite<Entity> lite = entity as Lite<Entity>;
        ");
    }

    [TestMethod]
    public void IsEntity()
    {
        TestDiagnostic("Impossible to convert Lite<T> to T, consider using Entity or Retrieve", @"
Lite<Entity> lite = null;
var result = lite is Entity;       
        ");
    }

    [TestMethod]
    public void IsLite()
    {

        TestDiagnostic("Impossible to convert T to Lite<T>, consider using ToLite or ToLiteFat", @"
Entity entity = null;
var result = entity is Lite<Entity>;
        ");
    }

    [TestMethod]
    public void IsEntityEntity()
    {
        TestDiagnostic("Impossible to convert Lite<T> to T, consider using Entity or Retrieve", @"
Lite<Entity> lite = null;
var result = lite is Entity entity;       
        ");
    }

    [TestMethod]
    public void IsEntityEntityCaseStatement()
    {
        TestDiagnostic("Impossible to convert Lite<T> to T, consider using Entity or Retrieve", @"
Lite<Entity> lite = null;
switch(lite)
{
case Entity entity: break;
}     
        ");
    }

    [TestMethod]
    public void IsEntityEntityCaseExpression()
    {
        TestDiagnostic("Impossible to convert Lite<T> to T, consider using Entity or Retrieve", @"
Lite<Entity> lite = null;
var a = lite switch
{
Entity entity=> true,
_ => false
};
        ");
    }

    [TestMethod]
    public void IsNotEntity()
    {
        TestDiagnostic("Impossible to convert Lite<T> to T, consider using Entity or Retrieve", @"
Lite<Entity> lite = null;
var result = lite is not Signum.Entities.Entity;       
        ");
    }

    [TestMethod]
    public void IsLiteOrLite()
    {

        TestDiagnostic("Impossible to convert T to Lite<T>, consider using ToLite or ToLiteFat", @"
Entity entity = null;
var result = entity is Lite<Entity> or Signum.Operations.OperationSymbol;
        ");
    }

    [TestMethod]
    public void IsNotLiteOrLite()
    {

        TestDiagnostic("Impossible to convert T to Lite<T>, consider using ToLite or ToLiteFat", @"
Entity entity = null;
var result = entity is not (Lite<Entity> or Signum.Operations.OperationSymbol);
        ");
    }

    [TestMethod]
    public void IsNotLite()
    {

        TestDiagnostic("Impossible to convert T to Lite<T>, consider using ToLite or ToLiteFat", @"
Entity entity = null;
var result = entity is not Lite<Entity>;
        ");
    }


    [TestMethod]
    public void IsLiteProperties()
    {
        TestDiagnostic("Impossible to convert T to Lite<T>, consider using ToLite or ToLiteFat", @"
Entity entity = null;
var result = entity is Lite<Signum.Operations.OperationLogEntity> { EntityOrNull: Signum.Operations.OperationLogEntity };
        ");
    }

    [TestMethod]
    public void IsEntityPropertiesLite()
    {
        TestDiagnostic("Impossible to convert Lite<T> to T, consider using Entity or Retrieve", @"
Entity entity = null;
var result = entity is Signum.Operations.OperationLogEntity { Target: Entity };
        ");
    }

    private void TestDiagnostic(string expectedError, string code, bool withIncludes = false, bool assertNoErrors = true)
    {
        string test = Surround(code, withIncludes: withIncludes);
        if (expectedError == null)
            VerifyCSharpDiagnostic(test, assertNoErrors, Array.Empty<DiagnosticResult>());
        else
            VerifyCSharpDiagnostic(test, assertNoErrors, new DiagnosticResult
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
