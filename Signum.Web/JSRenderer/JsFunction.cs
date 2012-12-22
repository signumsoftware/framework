using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;
using System.Linq.Expressions;

namespace Signum.Web
{
    public class JsFunction : JsRenderer, IEnumerable<JsInstruction>
    {
        List<JsInstruction> instructions = new List<JsInstruction>();

        public string[] Args { get; private set; }

        public JsFunction(params string[] args)
        {
            this.Args = args;

            Renderer = () => "function({0}){{{1}}}".Formato(Args.ToString(", "), instructions.ToString(a => a.ToJS(), ";").Indent(3));
        }

        public JsFunction Add(JsInstruction instruction)
        {
            instructions.Add(instruction);
            return this;
        }
     
        public IEnumerator<JsInstruction> GetEnumerator()
        {
            return instructions.GetEnumerator(); 
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return instructions.GetEnumerator(); 
        }

        public static implicit operator JsFunction(JsInstruction instruction)
        {
            return new JsFunction() { instruction }; 
        }
    }

    public class Js
    {
        public static JsInstruction Return<T>(JsValue<T> value)
        {
            return "return {0}".Formato(value.ToJS());
        }

        public static string NewPrefix(string prefix)
        {
            return TypeContextUtilities.Compose("New", prefix);
        }

        public static JsInstruction OpenTypeChooser(JsValue<string> prefix, JsFunction onOptionChosen, string[] typeNames)
        {
            return "SF.openTypeChooser({0}, {1}, {{controllerUrl:'{2}', types:'{3}'}});".Formato(
                    prefix.ToJS(),
                    onOptionChosen.ToJS(),
                    RouteHelper.New().SignumAction("GetTypeChooser"),
                    typeNames == null ? "" : typeNames.ToString(","));
        }

        public static JsInstruction OpenChooser(JsValue<string> prefix, JsFunction onOptionChosen, string[] optionNames)
        {
            return "SF.openChooser({0}, {1}, [{2}], null, {{controllerUrl:'{3}'}});".Formato(
                    prefix.ToJS(),
                    onOptionChosen.ToJS(),
                    optionNames.ToString(on => "'{0}'".Formato(on), ","),
                    RouteHelper.New().SignumAction("GetChooser"));
        }

        public static JsInstruction Submit<T>(Expression<Action<T>> action) where T : Controller
        {
            return Submit(RouteHelper.New().Action(action));
        }

        public static JsInstruction Submit<T>(Expression<Action<T>> action, JsInstruction requestExtraJsonData) where T:Controller
        {
            return Submit(RouteHelper.New().Action(action), requestExtraJsonData);
        }

        public static JsInstruction Submit(JsValue<string> controllerUrl)
        {
            return new JsInstruction(() => "SF.submit({0})".Formato(controllerUrl.ToJS()));
        }

        public static JsInstruction Submit(JsValue<string> controllerUrl, JsInstruction requestExtraJsonData)
        {
            if (requestExtraJsonData == null)
                return Submit(controllerUrl);

            return new JsInstruction(() => "SF.submit({0},{1})".Formato(controllerUrl.ToJS(), requestExtraJsonData.ToJS()));
        }

        public static JsInstruction SubmitOnly<T>(Expression<Action<T>> action, JsInstruction requestExtraJsonData) where T : Controller
        {
            return SubmitOnly(RouteHelper.New().Action(action), requestExtraJsonData);
        }

        public static JsInstruction SubmitOnly(JsValue<string> controllerUrl, JsInstruction requestExtraJsonData)
        {
            if (requestExtraJsonData == null)
                throw new ArgumentException("requestExtraJsonData must be given to SubmitOnly. Use Submit otherwise");

            return new JsInstruction(() => "SF.submitOnly({0},{1})".Formato(controllerUrl.ToJS(), requestExtraJsonData.ToJS()));
        }

        public static JsInstruction AjaxCall<T>(Expression<Action<T>> action, JsInstruction requestData, JsFunction onSuccess) where T : Controller
        {
            return AjaxCall(RouteHelper.New().Action(action), requestData, onSuccess);
        }

        public static JsInstruction AjaxCall(JsValue<string> controllerUrl, JsInstruction requestData, JsFunction onSuccess)
        {
            return new JsInstruction(() => "$.ajax({{url:{0},data:{1},success:{2}}})"
                .Formato(controllerUrl.ToJS(), requestData.ToJS(), onSuccess.TryCC(os => os.ToJS()) ?? "null"));
        }

        public static JsInstruction Confirm(JsValue<string> message, JsFunction onSuccess)
        {
            return new JsInstruction(() => "if(confirm({0})){1}()".Formato(message.ToJS(), onSuccess));
        }

        public static JsInstruction Confirm(JsValue<string> message, JsInstruction onSuccess)
        {
            return new JsInstruction(() => "if(confirm({0})) {1}".Formato(message.ToJS(), onSuccess.ToJS()));
        }

        public static JsInstruction SerializePopup(string prefix)
        {
            return new JsInstruction(() => "SF.Popup.serializeJson('{0}')".Formato(prefix));
        }

        public static JsInstruction SerializePopupFunction(string prefix)
        {
            return new JsInstruction(() => "function() {{ return SF.Popup.serializeJson('{0}'); }}".Formato(prefix));
        }
    }
}
