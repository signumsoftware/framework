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
    public class AutoPropertyTest : CodeFixVerifier
    {
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new AutoPropertyCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AutoPropertyAnalyzer();
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestMethod2()
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
    class MyEntity : Entity
    {   
        [NotNullable, SqlDbType(Size = 24)]
        string phone;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 24), TelephoneValidator]
        public string Phone
        {
            get { return phone; }
            set { Set(ref phone, value); }
        }
    }
}";

            VerifyCSharpDiagnostic(test, new DiagnosticResult
            {
                Id = AutoPropertyAnalyzer.DiagnosticId,
                Message = String.Format("Property '{0}' could be transformed to auto-property", "Phone"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 16, 9)
                }
            });

            var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Signum.Entities

namespace ConsoleApplication1
{
    class MyEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 24)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 24), TelephoneValidator]
        public string Phone { get; set; }
    }
}";
            VerifyCSharpFix(test, fixtest);
        }


        [TestMethod]
        public void TestMethod3()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Signum.Entities;
using Signum.Utilities;
using System.Linq.Expressions;

namespace ConsoleApplication1
{
    class MyEntity : Entity
    {   
        [NotNullable, SqlDbType(Size = 24)]
        string phone;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 24), TelephoneValidator]
        public string Phone
        {
            get { return phone; }
            set { Set(ref phone, value); }
        }

        [NotNullable, SqlDbType(Size = 24)]
        string phone2;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 24), TelephoneValidator]
        public string Phone2
        {
            get { return phone2; }
            set { SetToStr(ref phone2, value); }
        }

        int number;
        public int Number
        {
            get { return number; }
            set { Set(ref number, value); }
        }

        static Expression<Func<MyEntity, string>> ToStringExpressions =
            entity => entity.phone2;
        public override string ToString()
        {
            return ToStringExpressions.Evaluate(this);
        }
    }
}";

            VerifyCSharpDiagnostic(test, new DiagnosticResult
            {
                Id = AutoPropertyAnalyzer.DiagnosticId,
                Message = String.Format("Property '{0}' could be transformed to auto-property", "Phone"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 19, 23)
                }
            }, new DiagnosticResult
            {
                Id = AutoPropertyAnalyzer.DiagnosticId,
                Message = String.Format("Property '{0}' could be transformed to auto-property", "Phone2"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 28, 23)
                }
            }, new DiagnosticResult
            {
                Id = AutoPropertyAnalyzer.DiagnosticId,
                Message = String.Format("Property '{0}' could be transformed to auto-property", "Number"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 35, 20)
                }
            });

            var fixtest = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Signum.Entities;
using Signum.Utilities;
using System.Linq.Expressions;

namespace ConsoleApplication1
{
    class MyEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 24)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 24), TelephoneValidator]
        public string Phone { get; set; }

        [NotNullable, SqlDbType(Size = 24)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 24), TelephoneValidator]
        public string Phone2 { get; set; }

        public int Number { get; set; }

        static Expression<Func<MyEntity, string>> ToStringExpressions =
            entity => entity.Phone2;
        public override string ToString()
        {
            return ToStringExpressions.Evaluate(this);
        }
    }
}";
            VerifyCSharpFix(test, fixtest);
        }


    }
}