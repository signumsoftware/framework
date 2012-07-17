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
        public JsValidatorOptions ValidationOptions { get; set; }
        public JsFunction OnOk { get; set; }
        public JsFunction OnSave { get; set; }
        public JsFunction OnOkClosed { get; set; }
        public JsFunction OnCancelled { get; set; }
        public JsValue<string> Type { get; set; }
        public JsValue<int?> Id { get; set; }
        public JsValue<string> PartialViewName { get; set; }
        public JsValue<bool> Navigate { get; set; }
        public JsInstruction RequestExtraJsonData { get; set; }

        public JsViewOptions()
        {
            Renderer = () =>
            {
                return new JsOptionsBuilder(false)
                {
                    {"prefix", Prefix.TryCC(a=>a.ToJS())},
                    {"containerDiv", ContainerDiv.TryCC(a=>a.ToJS())},
                    {"controllerUrl", ControllerUrl.TryCC(a=>a.ToJS()) },
                    {"validationOptions", ValidationOptions.TryCC(a => a.ToJS()) },
                    {"onOk", OnOk.TryCC(a=>a.ToJS())},
                    {"onSave", OnSave.TryCC(a=>a.ToJS())},
                    {"onOkClosed", OnOkClosed.TryCC(a=>a.ToJS())},
                    {"onCancelled", OnCancelled.TryCC(a=>a.ToJS())},
                    {"type", Type.TryCC(a=>a.ToJS())},
                    {"partialViewName", PartialViewName.TryCC(a=>a.ToJS())},
                    {"navigate", Navigate.TryCC(a=>a.ToJS())},
                    {"requestExtraJsonData", RequestExtraJsonData.TryCC(a=>a.ToJS())},
                }.ToJS();
            };
        }
    }

    public class JsViewNavigator : JsInstruction
    {
        JsViewOptions Options { get; set; }

        public JsViewNavigator(JsViewOptions options)
            : base(() => "new SF.ViewNavigator(" + options.ToJS() + ")")
        {
            this.Options = options;
        }

        public JsInstruction viewOk()
        {
            return new JsInstruction(() => "{0}.viewOk()".Formato(this.ToJS()));
        }

        public JsInstruction createOk()
        {
            return new JsInstruction(() => "{0}.createOk()".Formato(this.ToJS()));
        }

        public JsInstruction viewSave(JsValue<string> html)
        {
            return new JsInstruction(() => "{0}.viewSave({1})".Formato(this.ToJS(), html.ToJS()));
        }

        public JsInstruction createSave(string saveUrl)
        {
            return new JsInstruction(() => "{0}.createSave('{1}')".Formato(this.ToJS(), saveUrl));
        }

        public static JsInstruction closePopup(string prefix)
        {
            return new JsInstruction(() => "SF.closePopup('{0}')".Formato(prefix));
        }
    }
}
