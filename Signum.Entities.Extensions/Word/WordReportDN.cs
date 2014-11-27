using Signum.Entities.Basics;
using Signum.Entities.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Word
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class WordReportTemplateDN : Entity
    {
        [NotNullable]
        TypeDN type;
        [NotNullValidator]
        public TypeDN Type
        {
            get { return type; }
            set { Set(ref type, value); }
        }

        [NotNullable]
        QueryDN query;
        [NotNullValidator]
        public QueryDN Query
        {
            get { return query; }
            set { Set(ref query, value); }
        }

        [NotNullable]
        Lite<FileDN> template;
        [NotNullValidator]
        public Lite<FileDN> Template
        {
            get { return template; }
            set { Set(ref template, value); }
        }
    }

    public static class WordReportOperation
    {
        public static readonly ExecuteSymbol<WordReportTemplateDN> Save = OperationSymbol.Execute<WordReportTemplateDN>();
    }
    
}
