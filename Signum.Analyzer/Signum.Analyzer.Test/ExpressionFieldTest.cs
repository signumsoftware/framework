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
            return new AutoPropertyCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ExpressionFieldAnalyzer();
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void ExpressionFieldNoReturnType()
        {
            Test("no return type", @"        
        [ExpressionField]
        public static void OperationLogs(this Entity e)
        {
        }");
        }

        [TestMethod]
        public void ExpressionComplexParameter()
        {
            Test("complex paramerer 'e'", @"        
        [ExpressionField]
        public static int OperationLogs(ref Entity e)
        {
            return 0;
        }");
        }

        [TestMethod]
        public void Expression2Statements()
        {
            Test("2 statements", @"        
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
            Test("no return", @"        
        [ExpressionField]
        public static int OperationLogs(this Entity e)
        {
            var a = 2;
        }");
        }

        [TestMethod]
        public void ExpressionNoReturnExpression()
        {
            Test("no return expression", @"        
        [ExpressionField]
        public static int OperationLogs(this Entity e)
        {
            return;
        }");
        }

        [TestMethod]
        public void ExpressionNoGetter()
        {
            Test("no getter", @"        
        [ExpressionField]
        public static int OperationLogs { set; }");
        }

        [TestMethod]
        public void ExpressionNoGetterBody()
        {
            Test("no getter body", @"        
        [ExpressionField]
        public static int OperationLogs { get; }");
        }

        [TestMethod]
        public void ExpressionMethodExpressionBody()
        {
            Test("no invocation", @"        
        [ExpressionField]
        public static int OperationLogs => 1");
        }

        [TestMethod]
        public void ExpressionGetterExpressionBody()
        {
            Test("no invocation", @"        
        [ExpressionField]
        public static int OperationLogs(this Entity e) => 1");
        }

        [TestMethod]
        public void ExpressionNoEvaluate()
        {
            Test("no Evaluate", @"
        static Expression<Entity, int> MyExpression;         
        [ExpressionField]
        public static int OperationLogs(this Entity e) => MyExpression.Invoke(e)");
        }

        [TestMethod]
        public void ExpressionNoStaticField()
        {
            Test("no static field", @"
        static Expression<Entity, int> MyExpression { get; }         
        [ExpressionField]
        public static int OperationLogs(this Entity e) => MyExpression.Evaluate(e)");
        }

        [TestMethod]
        public void ExpressionThis()
        {
            Test("first argument should be 'this'", @"
        static Expression<Entity, int> MyExpression;        
        [ExpressionField]
        public int OperationLogs(this Entity e) => MyExpression.Evaluate(e, e)");
        }

        [TestMethod]
        public void ExpressionMissingArgument()
        {
            Test("missing argument 'e'", @"
        static Expression<Bla, int> MyExpression;        
        [ExpressionField]
        public int OperationLogs(this Entity e) => MyExpression.Evaluate(this)");
        }

        [TestMethod]
        public void ExpressionExtra()
        {
            Test("extra parameters", @"
        static Expression<Bla, Entity, Entity, int> MyExpression;        
        [ExpressionField]
        public int OperationLogs(this Entity e) => MyExpression.Evaluate(this, e, e)");
        }

        [TestMethod]
        public void ExpressionCorrect()
        {
            Test(null, @"
        static Expression<Bla, Entity, int> MyExpression;        
        [ExpressionField]
        public int OperationLogs(this Entity e) => MyExpression.Evaluate(this, e)");
        }

        private void Test(string text, string expression)
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Signum.Entities;

namespace ConsoleApplication1
{
    static class Bla
    {   
" + expression +
@"
    }
}";

            if (text == null)
                VerifyDiagnostic(test, new DiagnosticResult[0]);
            else
                VerifyDiagnostic(test, new DiagnosticResult
                {
                    Id = ExpressionFieldAnalyzer.DiagnosticId,
                    Message = string.Format("'OperationLogs' should be a simple evaluation of an static Expression<T> field with the same signature ({0})", text),
                    Severity = DiagnosticSeverity.Warning,
                });
        }


        //        [TestMethod]
        //        public void TestMethod3()
        //        {
        //            var test = @"
        //using System;
        //using System.Collections.Generic;
        //using System.Linq;
        //using System.Text;
        //using System.Threading.Tasks;
        //using System.Diagnostics;
        //using Signum.Entities;
        //using Signum.Utilities;
        //using System.Linq.Expressions;

        //namespace ConsoleApplication1
        //{
        //    class MyEntity : Entity
        //    {
        //        [NotNullable, SqlDbType(Size = 24)]
        //        string phone;
        //        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 24), TelephoneValidator]
        //        public string Phone
        //        {
        //            get { return phone; }
        //            set { Set(ref phone, value); }
        //        }

        //        [NotNullable, SqlDbType(Size = 24)]
        //        string phone2;
        //        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 24), TelephoneValidator]
        //        public string Phone2
        //        {
        //            get { return phone2; }
        //            internal set { SetToStr(ref phone2, value); }
        //        }

        //        int number;
        //        public int Number
        //        {
        //            get { return number; }
        //            set { Set(ref number, value); }
        //        }

        //        DateTime creationDate = TimeZoneManager.Now;
        //        public DateTime CreationDate
        //        {
        //            get { return creationDate; }
        //            private set { Set(ref creationDate, value); }
        //        }

        //        int? synchronizeSchema;       
        //        [Unit(""ms"")]
        //        public int? SynchronizeSchema
        //        {
        //            get { return synchronizeSchema; }
        //            set { Set(ref synchronizeSchema, value); }
        //        }

        //        static Expression<Func<MyEntity, string>> ToStringExpressions =
        //            entity => entity.phone2;
        //        public override string ToString()
        //        {
        //            return ToStringExpressions.Evaluate(this);
        //        }
        //    }
        //}";

        //            VerifyDiagnostic(test, new DiagnosticResult
        //            {
        //                Id = AutoPropertyAnalyzer.DiagnosticId,
        //                Message = "Properties in 'MyEntity' could be transformed to auto-property",
        //                Severity = DiagnosticSeverity.Warning
        //            });

        //            var fixtest = @"
        //using System;
        //using System.Collections.Generic;
        //using System.Linq;
        //using System.Text;
        //using System.Threading.Tasks;
        //using System.Diagnostics;
        //using Signum.Entities;
        //using Signum.Utilities;
        //using System.Linq.Expressions;

        //namespace ConsoleApplication1
        //{
        //    class MyEntity : Entity
        //    {
        //        [NotNullable, SqlDbType(Size = 24)]
        //        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 24), TelephoneValidator]
        //        public string Phone { get; set; }

        //        [NotNullable, SqlDbType(Size = 24)]
        //        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 24), TelephoneValidator]
        //        public string Phone2 { get; internal set; }

        //        public int Number { get; set; }

        //        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        //        [Unit(""ms"")]
        //        public int? SynchronizeSchema { get; set; }

        //        static Expression<Func<MyEntity, string>> ToStringExpressions =
        //            entity => entity.Phone2;
        //        public override string ToString()
        //        {
        //            return ToStringExpressions.Evaluate(this);
        //        }
        //    }
        //}";
        //            VerifyFix(test, fixtest);
        //        }


    }
}