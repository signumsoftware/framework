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
                return new JsOptionsBuilder(false)
                {
                    {"prefix", Prefix.TrySingleQuote()},
                    {"containerDiv", ContainerDiv.TrySingleQuote()},
                    {"controllerUrl", ControllerUrl.TrySingleQuote()},
                    {"onOk", OnOk},
                    {"onOkClosed", OnOkClosed},
                    {"onCancelled", OnCancelled},
                    {"type", Type.TrySingleQuote()},
                    {"partialViewName", PartialViewName.TrySingleQuote()},
                    {"requestExtraJsonData", RequestExtraJsonData},
                }.ToJS();
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
