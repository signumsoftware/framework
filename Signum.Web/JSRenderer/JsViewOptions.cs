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
        public JsValue<string> Prefix { get; set; }
        public JsValue<string> ContainerDiv { get; set; }
        public JsValue<string> ControllerUrl { get; set; }
        public JsValue<string> ValidationControllerUrl { get; set; }
        public JsFunction OnOk { get; set; }
        public JsFunction OnOkClosed { get; set; }
        public JsFunction OnCancelled { get; set; }
        public JsValue<string> Type { get; set; }
        public JsValue<int?> Id { get; set; }
        public JsValue<string> PartialViewName { get; set; }
        public JsInstruction RequestExtraJsonData { get; set; }

        public JsViewOptions()
        {
            Renderer = () =>
            {
                return new JsOptionsBuilder(false)
                {
                    {"prefix", Prefix.TryCC(a=>a.ToJS())},
                    {"containerDiv", ContainerDiv.TryCC(a=>a.ToJS())},
                    {"controllerUrl", ControllerUrl.TryCC(a=>a.ToJS()) ?? RouteHelper.New().SignumAction("PopupView").SingleQuote()},
                    {"validationControllerUrl", ValidationControllerUrl.TryCC(a => a.ToJS()) },
                    {"onOk", OnOk.TryCC(a=>a.ToJS())},
                    {"onOkClosed", OnOkClosed.TryCC(a=>a.ToJS())},
                    {"onCancelled", OnCancelled.TryCC(a=>a.ToJS())},
                    {"type", Type.TryCC(a=>a.ToJS())},
                    {"partialViewName", PartialViewName.TryCC(a=>a.ToJS())},
                    {"requestExtraJsonData", RequestExtraJsonData.TryCC(a=>a.ToJS())},
                }.ToJS();
            };
        }
    }

    public class JsViewNavigator : JsInstruction
    {
        JsViewOptions Options { get; set; }

        public JsViewNavigator(JsViewOptions options)
            : base(() => "new ViewNavigator(" + options.ToJS() + ")")
        {
            this.Options = options;
        }

        public JsInstruction ShowCreateSave(JsValue<string> html)
        {
            return new JsInstruction(() => "{0}.{1}({2})".Formato(this.ToJS(), "showCreateSave", html.ToJS()));
        }

        public static JsInstruction ClosePopup(string prefix)
        {
            return new JsInstruction(() => "ClosePopup('{0}')".Formato(prefix));
        }
    }
}
