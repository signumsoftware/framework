using System.ComponentModel;

namespace Signum.Templating;

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
    YouCannotAddBlocksWithAllOrAny,
    [Description("Impossible to access {0} because the template has no {1}")]
    ImpossibleToAccess0BecauseTheTemplateHAsNo1,
}

public enum TemplateMessage
{
    Template,
    [Description("Copy to clipboard: Ctrl+C, ESC")]
    CopyToClipboard,
}
