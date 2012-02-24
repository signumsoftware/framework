using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;
using Signum.Engine.Operations;
using Signum.Entities.Basics;

namespace Signum.Web.Operations
{
    public class JsOperationOptions : JsRenderer
    {
        public JsValue<string> Prefix { get; set; }
        public JsValue<string> ParentDiv { get; set; }
        public Enum Operation { get; set; }
        public JsValue<bool?> IsLite { get; set; }
        public JsValue<bool?> IsContextual { get; set; }
        public JsValue<string> ReturnType { get; set; }
        public JsValue<string> ControllerUrl { get; set; }
        public JsValidatorOptions ValidationOptions { get; set; }
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

                JsValidatorOptions valOptions = ValidationOptions ?? new JsValidatorOptions();
                bool emptyValOptionsController = valOptions.ControllerUrl == null;
                if (!emptyValOptionsController)
                {
                    string valController = valOptions.ControllerUrl.ToJS();
                    emptyValOptionsController = string.IsNullOrEmpty(valController) || valController == "''";
                }
                if (emptyValOptionsController)
                    valOptions.ControllerUrl = RouteHelper.New().SignumAction(emptyPrefix ? "Validate" : "ValidatePartial");

                if (IsLite == null && Operation != null)
                {
                    IsLite = OperationLogic.IsLite(Operation);
                }

                var builder = new JsOptionsBuilder(false)
                {
                    {"sender", "this"},
                    {"prefix", Prefix.TryCC(a=>a.ToJS())},
                    {"parentDiv", ParentDiv.TryCC(a => a.ToJS())},
                    {"operationKey", Operation.TryCC(o => ((JsValue<string>)EnumDN.UniqueKey(o))).TryCC(a=>a.ToJS())},
                    {"isLite", IsLite.TryCC(a=>a.ToJS())},
                    {"contextual", IsContextual.TryCC(a=>a.ToJS())},
                    {"returnType",ReturnType.TryCC(a=>a.ToJS())},
                    {"requestExtraJsonData", RequestExtraJsonData.TryCC(a=>a.ToJS())},
                    {"controllerUrl", ControllerUrl.TryCC(a=>a.ToJS())},
                    {"validationOptions", valOptions.ToJS()}
                };

                return builder.ToJS();
            };
        }
    }
}
