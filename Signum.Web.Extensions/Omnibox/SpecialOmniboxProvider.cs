using Signum.Entities.Omnibox;
using Signum.Utilities;
using Signum.Web;
using Signum.Web.Omnibox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace Signum.Web.Omnibox
{
    public class SpecialOmniboxAction : ISpecialOmniboxAction
    {
        public SpecialOmniboxAction(string key, Func<bool> allowed, Func<UrlHelper, string> onClick)
        {
            this.Key = key;
            this.Allowed = allowed;
            this.OnClick = onClick;
        }

        public string Key { get; private set; }
        public Func<bool> Allowed { get; private set; }
        public Func<UrlHelper, string> OnClick { get; private set; }
    }

    public class SpecialOmniboxProvider : OmniboxClient.OmniboxProvider<SpecialOmniboxResult>
    {
        public static Dictionary<string, SpecialOmniboxAction> Actions = new Dictionary<string, SpecialOmniboxAction>();

        public static void Register(SpecialOmniboxAction action)
        {
            Actions.AddOrThrow(action.Key, action, "SpecialOmniboxAction {0} already registered"); 
        }

        public override OmniboxResultGenerator<SpecialOmniboxResult> CreateGenerator()
        {
            return new SpecialOmniboxGenerator<SpecialOmniboxAction> { Actions = Actions };
        }

        public override MvcHtmlString RenderHtml(SpecialOmniboxResult result)
        {
            var html = Icon().Concat("!{0}".FormatHtml(result.Match.ToHtml()));

            return html;
        }

        public override string GetUrl(SpecialOmniboxResult result)
        {
            return ((SpecialOmniboxAction)result.Match.Value).OnClick(RouteHelper.New()); 
        }

        public override MvcHtmlString Icon()
        {
            return ColoredGlyphicon("glyphicon-cog", "limegreen");
        }
    }

}
