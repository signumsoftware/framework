using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;

namespace Signum.Web.Operations
{
    public class JsOperationOptions : JsRenderer
    {
        public JsValue<string> Prefix { get; set; }
        public JsValue<string> ParentDiv { get; set; }
        public JsValue<string> OperationKey { get; set; }
        public JsValue<bool?> IsLite { get; set; }
        public JsValue<bool?> IsContextual { get; set; }
        public JsValue<string> ReturnType { get; set; }
        public JsValue<string> ControllerUrl { get; set; }
        public JsValue<string> ValidationControllerUrl { get; set; }
        public JsInstruction RequestExtraJsonData { get; set; }

        public JsOperationOptions()
        {
            Renderer = () =>
            {
                bool emptyPrefix = false;
                if (Prefix == null)
                    emptyPrefix = true;
                else
                {
                    string pf = Prefix.ToJS();
                    if (string.IsNullOrEmpty(pf) || pf == "''")
                        emptyPrefix = true;
                }
                    
                var builder = new JsOptionsBuilder(false)
                {
                    {"prefix", Prefix.TryCC(a=>a.ToJS())},
                    {"parentDiv", ParentDiv.TryCC(a => a.ToJS())},
                    {"operationKey", OperationKey.TryCC(a=>a.ToJS())},
                    {"isLite", IsLite.TryCC(a=>a.ToJS())},
                    {"contextual", IsContextual.TryCC(a=>a.ToJS())},
                    {"returnType",ReturnType.TryCC(a=>a.ToJS())},
                    {"requestExtraJsonData", RequestExtraJsonData.TryCC(a=>a.ToJS())},
                    {"controllerUrl", ControllerUrl.TryCC(a=>a.ToJS())},
                    {"validationControllerUrl", ValidationControllerUrl.TryCC(a => a.ToJS()) ?? 
                        RouteHelper.New().SignumAction(emptyPrefix ? "Validate" : "ValidatePartial").SingleQuote() }
                };

                return builder.ToJS();
            };
        }
    }
}
