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
        [NotNullable, UniqueIndex]
        public string FullClassName { get; set; }

        static readonly Expression<Func<SystemWordTemplateEntity, string>> ToStringExpression = e => e.FullClassName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [Serializable]
    public class MultiEntityModel : ModelEntity
    {
        [NotNullable, ImplementedByAll]
        [NotNullValidator, NoRepeatValidator]
        public MList<Lite<Entity>> Entities { get; set; } = new MList<Lite<Entity>>();
    }

    [Serializable]
    public class QueryModel : ModelEntity
    {
        [NotNullValidator, InTypeScript(false)]
        public object QueryName { get; set; }

        [InTypeScript(false)]
        public List<Filter> Filters { get; set; } = new List<Filter>();
        
        [InTypeScript(false)]
        public List<Order> Orders { get; set; } = new List<Order>();

        [NotNullValidator, InTypeScript(false)]
        public Pagination Pagination { get; set; }
    }

    public enum QueryModelMessage
    {
        [Description("Configure your query and press [Search] before [Ok]")]
        ConfigureYourQueryAndPressSearchBeforeOk
    }
}
