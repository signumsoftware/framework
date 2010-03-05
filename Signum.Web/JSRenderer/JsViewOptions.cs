using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;

namespace Signum.Web
{
    public class JsViewOptions : JsRenderer
    {
        public string Prefix { get; set; }
        public string ContainerDiv { get; set; }
        public string ControllerUrl { get; set; }
        public string OnOk { get; set; }
        public string OnOkClosed { get; set; }
        public string OnCancelled { get; set; }
        public string Type { get; set; }
        public int? Id { get; set; }
        public string PartialViewName { get; set; }
        public string RequestExtraJsonData { get; set; }

        public JsViewOptions()
        {
            renderer = () =>
            {
                StringBuilder sb = new StringBuilder();

                if (Prefix.HasText())
                    sb.Append("prefix:'{0}',".Formato(Prefix));

                if (ContainerDiv.HasText())
                    sb.Append("containerDiv:'{0}',".Formato(ContainerDiv));

                if (ControllerUrl.HasText())
                    sb.Append("controllerUrl:'{0}',".Formato(ControllerUrl));

                if (OnOk.HasText())
                    sb.Append("onOk:{0},".Formato(OnOk));

                if (OnOkClosed.HasText())
                    sb.Append("onOkClosed:{0},".Formato(OnOkClosed));

                if (OnCancelled.HasText())
                    sb.Append("onCancelled:{0},".Formato(OnCancelled));

                if (Type.HasText())
                    sb.Append("type:'{0}',".Formato(Type));

                if (Id.HasValue)
                    sb.Append("id:'{0}',".Formato(Id.Value.ToString()));

                if (PartialViewName.HasText())
                    sb.Append("partialViewName:'{0}',".Formato(PartialViewName));

                if (RequestExtraJsonData.HasText())
                    sb.Append("requestExtraJsonData:{0},".Formato(RequestExtraJsonData));

                string result = sb.ToString();

                return result.HasText() ? 
                    "{" + result.Substring(0, result.Length - 1) + "}" :
                    null; //Instead of "" so we can use Combine string extension
            };
        }
    }

    public class JsViewNavigator
    {
        public static JsRenderer JsOpenChooser(string prefix, string onOptionChosen, string[] optionNames)
        {
            return new JsRenderer(() =>
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("openChooser('{0}', {1}, [{2}]);".Formato(
                    prefix,
                    onOptionChosen,
                    optionNames.ToString(on => "'{0}'".Formato(on), ",")
                    ));

                return sb.ToString();
            });
        }
    }
}
