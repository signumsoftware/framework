using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Templating
{
    public enum TemplateTokenMessage
    {
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
}
