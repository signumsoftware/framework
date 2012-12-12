using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Extensions.Properties;
using Signum.Utilities;
using Signum.Entities.Files;
using System.Linq.Expressions;

namespace Signum.Entities.Reports
{
    [Serializable]
    public class ExcelReportDN : IdentifiableEntity
    {
        QueryDN query;
        [NotNullValidator]
        public QueryDN Query
        {
            get { return query; }
            set { Set(ref query, value, () => Query); }
        }
  
        [NotNullable]
        string displayName;
        [StringLengthValidator(Min = 3)]
        public string DisplayName
        {
            get { return displayName; }
            set { SetToStr(ref displayName, value, () => DisplayName); }
        }

        [NotNullable]
        EmbeddedFileDN file;
        [NotNullValidator]
        public EmbeddedFileDN File
        {
            get { return file; }
            set { Set(ref file, value, () => File); }
        }

        static readonly Expression<Func<ExcelReportDN, string>> ToStringExpression = e => e.displayName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public enum ExcelReportOperation
    { 
        Save,
        Delete
    }
}
