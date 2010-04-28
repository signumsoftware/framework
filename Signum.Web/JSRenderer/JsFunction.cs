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

        public static JsInstruction OpenChooser(JsValue<string> prefix, string[] optionNames, JsFunction onOptionChosen)
        {
            return "openChooser({0}, [{1}], {2});".Formato(
                    prefix.ToJS(),
                    optionNames.ToString(on => "'{0}'".Formato(on), ","),
                    onOptionChosen);
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

        public static JsInstruction Confirm(JsValue<string> message, JsFunction onSuccess)
        {
            return new JsInstruction(() => "if(confirm({0})){1}()".Formato(message.ToJS(), onSuccess));
        }

        public static JsInstruction ReloadEntity(JsValue<string> controllerUrl, JsValue<string> parentDiv)
        {
            return new JsInstruction(() => "ReloadEntity({0},{1})".Formato(controllerUrl.ToJS(), parentDiv.ToJS()));
        }
    }
}
