using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Main)]
    public class EmailTemplateDN : IdentifiableEntity
    {
        [NotNullable, UniqueIndex]
        string fullClassName;
        public string FullClassName
        {
            get { return fullClassName; }
            set { Set(ref fullClassName, value, () => FullClassName); }
        }

        [NotNullable]
        string friendlyName;
        [StringLengthValidator(Min = 1)]
        public string FriendlyName
        {
            get { return friendlyName; }
            set { Set(ref friendlyName, value, () => FriendlyName); }
        }

        static readonly Expression<Func<EmailTemplateDN, string>> ToStringExpression = e => e.friendlyName;
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
