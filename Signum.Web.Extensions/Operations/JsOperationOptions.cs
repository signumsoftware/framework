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
                if (IsLite == null && Operation != null)
                    IsLite = OperationLogic.IsLite(Operation);

                var builder = new JsOptionsBuilder(false)
                {
                    {"sender", "this"},
                    {"parentDiv", ParentDiv.TryCC(a => a.ToJS())},
                    {"operationKey", Operation.TryCC(o => ((JsValue<string>)MultiEnumDN.UniqueKey(o))).TryCC(a=>a.ToJS())},
                    {"contextual", IsContextual.TryCC(a=>a.ToJS())},
                    {"returnType",ReturnType.TryCC(a=>a.ToJS())},
                    {"requestExtraJsonData", RequestExtraJsonData.TryCC(a=>a.ToJS())},
                    {"validationOptions", ValidationOptions.TryCC(vo => vo.ToJS())}
                };

                var liteString = IsLite.TryCC(il => il.ToJS());
                if (liteString == "true")
                    builder.Add("isLite", liteString);
                
                var prefixString = Prefix.TryCC(p => p.ToJS());
                if (prefixString.HasText() && prefixString != "null" && prefixString != "''")
                    builder.Add("prefix", prefixString);

                var controllerUrlString = ControllerUrl.TryCC(c => c.ToJS());
                if (controllerUrlString.HasText() && controllerUrlString != "null" && controllerUrlString != "''")
                    builder.Add("controllerUrl", controllerUrlString);

                return builder.ToJS();
            };
        }
    }
}
