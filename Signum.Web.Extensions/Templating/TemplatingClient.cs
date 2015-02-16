using Signum.Entities.DynamicQuery;
using Signum.Entities.Templating;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.Web.Templating
{
    public class TemplatingClient
    {
        public static void TemplatingDecorators(QueryToken qt, HtmlTag option)
        {
            string canIf = CanIf(qt);
            if (canIf.HasText())
                option.Attr("data-if", canIf);

            string canForeach = CanForeach(qt);
            if (canForeach.HasText())
                option.Attr("data-foreach", canForeach);

            string canAny = CanAny(qt);
            if (canAny.HasText())
                option.Attr("data-any", canAny);
        }

        static string CanIf(QueryToken token)
        {
            if (token == null)
                return TemplateTokenMessage.NoColumnSelected.NiceToString();

            if (token.Type != typeof(string) && token.Type != typeof(byte[]) && token.Type.ElementType() != null)
                return TemplateTokenMessage.YouCannotAddIfBlocksOnCollectionFields.NiceToString();

            if (token.HasAllOrAny())
                return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.NiceToString();

            return null;
        }

        static string CanForeach(QueryToken token)
        {
            if (token == null)
                return TemplateTokenMessage.NoColumnSelected.NiceToString();

            if (token.Type != typeof(string) && token.Type != typeof(byte[]) && token.Type.ElementType() != null)
                return TemplateTokenMessage.YouHaveToAddTheElementTokenToUseForeachOnCollectionFields.NiceToString();

            if (token.Key != "Element" || token.Parent == null || token.Parent.Type.ElementType() == null)
                return TemplateTokenMessage.YouCanOnlyAddForeachBlocksWithCollectionFields.NiceToString();

            if (token.HasAllOrAny())
                return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.NiceToString();

            return null;
        }

        static string CanAny(QueryToken token)
        {
            if (token == null)
                return TemplateTokenMessage.NoColumnSelected.NiceToString();

            if (token.HasAllOrAny())
                return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.NiceToString();

            return null;
        }
    }
}