using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using System.ComponentModel;

namespace Signum.Entities.Word
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
    public class SystemWordTemplateEntity : Entity
    {
        [StringLengthValidator(AllowNulls = false, Max = 200), UniqueIndex]
        public string FullClassName { get; set; }

        static readonly Expression<Func<SystemWordTemplateEntity, string>> ToStringExpression = e => e.FullClassName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

}
