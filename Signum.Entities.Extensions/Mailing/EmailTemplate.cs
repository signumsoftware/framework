using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class EmailTemplateDN : IdentifiableEntity
    {
        [NotNullable, UniqueIndex]
        string fullClassName;
        public string FullClassName
        {
            get { return fullClassName; }
            set { Set(ref fullClassName, value, () => FullClassName); }
        }

        static readonly Expression<Func<EmailTemplateDN, string>> ToStringExpression = e => e.fullClassName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public enum EmailTemplateOperation
    { 
        Save
    }
}
