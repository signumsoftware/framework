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
        public void AutoPropTest1()
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

            VerifyDiagnostic(test, new DiagnosticResult
            {
                Id = AutoPropertyAnalyzer.DiagnosticId,
                Message = "Properties in 'MyEntity' could be transformed to auto-property",
                Severity = DiagnosticSeverity.Warning,
            });

            var fixtest = @"
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
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 24), TelephoneValidator]
        public string Phone { get; set; }
    }
}";
            VerifyFix(test, fixtest);
        }


        [TestMethod]
        public void AutoPropTest2()
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

        string bla;
        public string SuperPhone => Phone;

        [NotNullable, SqlDbType(Size = 24)]
        string phone2;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 24), TelephoneValidator]
        public string Phone2
        {
            get { return phone2; }
            internal set { SetToStr(ref phone2, value); }
        }

        int number;
        public int Number
        {
            get { return number; }
            set { Set(ref number, value); }
        }

        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value); }
        }

        int? synchronizeSchema;       
        [Unit(""ms"")]
        public int? SynchronizeSchema
        {
            get { return synchronizeSchema; }
            set { Set(ref synchronizeSchema, value); }
        }

        object queryName;
        [NotNullValidator]
        public object QueryName
        {
            get { return queryName; }
        }

        [NonSerialized]
        bool needNewQuery;
        public bool NeedNewQuery
        {
            get { return needNewQuery; }
            set { Set(ref needNewQuery, value); }
        }

        static Expression<Func<MyEntity, string>> ToStringExpressions =
            entity => entity.phone2;
        public override string ToString()
        {
            return ToStringExpressions.Evaluate(this);
        }
    }
}";

            VerifyDiagnostic(test, new DiagnosticResult
            {
                Id = AutoPropertyAnalyzer.DiagnosticId,
                Message = "Properties in 'MyEntity' could be transformed to auto-property",
                Severity = DiagnosticSeverity.Warning
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

        string bla;
        public string SuperPhone => Phone;

        [NotNullable, SqlDbType(Size = 24)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 24), TelephoneValidator]
        public string Phone2 { get; internal set; }

        public int Number { get; set; }

        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        [Unit(""ms"")]
        public int? SynchronizeSchema { get; set; }

        object queryName;
        [NotNullValidator]
        public object QueryName
        {
            get { return queryName; }
        }

        [NonSerialized]
        bool needNewQuery;
        public bool NeedNewQuery
        {
            get { return needNewQuery; }
            set { Set(ref needNewQuery, value); }
        }

        static Expression<Func<MyEntity, string>> ToStringExpressions =
            entity => entity.Phone2;
        public override string ToString()
        {
            return ToStringExpressions.Evaluate(this);
        }
    }
}";
            VerifyFix(test, fixtest);
        }
        

    }
}