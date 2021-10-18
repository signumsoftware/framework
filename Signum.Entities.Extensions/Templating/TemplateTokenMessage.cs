using Signum.Entities.DynamicQuery;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Signum.Entities.Templating
{
    public enum TemplateTokenMessage
    {
        Insert,
        [Description("No column selected")]
        NoColumnSelected,
        [Description("You cannot add If blocks on collection fields")]
        YouCannotAddIfBlocksOnCollectionFields,
        [Description("You have to add the Element token to use Foreach on collection fields")]
        YouHaveToAddTheElementTokenToUseForeachOnCollectionFields,
        [Description("You can only add Foreach blocks with collection fields")]
        YouCanOnlyAddForeachBlocksWithCollectionFields,
        [Description("You cannot add Blocks with All or Any")]
        YouCannotAddBlocksWithAllOrAny
    }


    [Serializable]
    public class MultiEntityModel : ModelEntity
    {
        [ImplementedByAll]
        [NoRepeatValidator]
        public MList<Lite<Entity>> Entities { get; set; } = new MList<Lite<Entity>>();
    }

    [Serializable]
    public class QueryModel : ModelEntity
    {
        [InTypeScript(false)]
        public object QueryName { get; set; }

        [InTypeScript(false)]
        public List<Filter> Filters { get; set; } = new List<Filter>();

        [InTypeScript(false)]
        public List<Order> Orders { get; set; } = new List<Order>();

        [InTypeScript(false)]
        public Pagination Pagination { get; set; }
    }

    public enum QueryModelMessage
    {
        [Description("Configure your query and press [Search] before [Ok]")]
        ConfigureYourQueryAndPressSearchBeforeOk
    }
}
