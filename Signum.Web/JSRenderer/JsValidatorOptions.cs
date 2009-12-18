using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;

namespace Signum.Web
{
    public class JsValidatorOptions : JsRenderer
    {
        public string Prefix { get; set; }
        public string ParentDiv { get; set; }
        public string ControllerUrl { get; set; }
        public string PrefixToIgnore { get; set; }

        public bool showInlineErrors = true;
        public bool ShowInlineErrors { get { return showInlineErrors; } set { showInlineErrors = value; } }

        public string fixedInlineErrorText = "*";
        public string FixedInlineErrorText { get { return fixedInlineErrorText; } set { fixedInlineErrorText = value; } }

        public string OnSuccess { get; set; }
        
        public string Type { get; set; }
        public int? Id { get; set; }
        public string RequestExtraJsonData { get; set; }

        public JsValidatorOptions()
        {
            renderer = () =>
            {
                StringBuilder sb = new StringBuilder();

                if (Prefix.HasText())
                    sb.Append("prefix:'{0}',".Formato(Prefix));

                if (ParentDiv.HasText())
                    sb.Append("parentDiv:'{0}',".Formato(ParentDiv));

                if (ControllerUrl.HasText())
                    sb.Append("controllerUrl:'{0}',".Formato(ControllerUrl));

                if (PrefixToIgnore.HasText())
                    sb.Append("prefixToIgnore:'{0}',".Formato(PrefixToIgnore));

                if (!ShowInlineErrors)
                    sb.Append("showInlineErrors:'false',");

                if (FixedInlineErrorText != "*")
                    sb.Append("fixedInlineErrorText:'{0}',".Formato(FixedInlineErrorText));

                if (OnSuccess.HasText())
                    sb.Append("onSuccess:{0},".Formato(OnSuccess));

                if (Type.HasText())
                    sb.Append("type:'{0}',".Formato(Type));

                if (Id.HasValue)
                    sb.Append("id:'{0}',".Formato(Id.Value.ToString()));

                if (RequestExtraJsonData.HasText())
                    sb.Append("requestExtraJsonData:{0},".Formato(RequestExtraJsonData));

                string result = sb.ToString();

                return result.HasText() ? 
                    "{" + result.Substring(0, result.Length - 1) + "}" :
                    null; //Instead of "" so we can use Combine string extension
            };
        }
    }
}
