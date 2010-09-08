using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

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

        public void Add(JsInstruction instruction)
        {
            instructions.Add(instruction);
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

        public static JsInstruction OpenChooser(JsValue<string> prefix, JsFunction onOptionChosen, string[] optionNames)
        {
            return "openChooser({0}, {1}, [{2}]);".Formato(
                    prefix.ToJS(),
                    onOptionChosen.ToJS(),
                    optionNames.ToString(on => "'{0}'".Formato(on), ","));
        }

        public static JsInstruction Submit(JsValue<string> controllerUrl)
        {
            return new JsInstruction(() => "Submit({0})".Formato(controllerUrl.ToJS()));
        }

        public static JsInstruction Submit(JsValue<string> controllerUrl, JsInstruction requestExtraJsonData)
        {
            if (requestExtraJsonData == null)
                return Submit(controllerUrl);

            return new JsInstruction(() => "Submit({0},{1})".Formato(controllerUrl.ToJS(), requestExtraJsonData.ToJS()));
        }

        public static JsInstruction SubmitOnly(JsValue<string> controllerUrl, JsInstruction requestExtraJsonData)
        {
            if (requestExtraJsonData == null)
                throw new ArgumentException("requestExtraJsonData must be given to SubmitOnly. Use Submit otherwise");

            return new JsInstruction(() => "SubmitOnly({0},{1})".Formato(controllerUrl.ToJS(), requestExtraJsonData.ToJS()));
        }

        public static JsInstruction AjaxCall(JsValue<string> controllerUrl, JsInstruction requestData, JsFunction onSuccess)
        {
            return new JsInstruction(() => "SF.ajax({{type:'POST',url:{0},async:false,data:{1},success:{2}}})"
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

        public static JsInstruction ReloadEntity(JsValue<string> controllerUrl, JsValue<string> parentDiv)
        {
            return new JsInstruction(() => "ReloadEntity({0},{1})".Formato(controllerUrl.ToJS(), parentDiv.ToJS()));
        }
    }
}
