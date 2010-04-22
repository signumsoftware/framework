using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;

namespace Signum.Web
{
    public class JsValidatorOptions : JsRenderer
    {
        public JsValue<string> Prefix { get; set; }
        public JsValue<string> ParentDiv { get; set; }
        public JsValue<string> ControllerUrl { get; set; }
        public JsValue<string> PrefixToIgnore { get; set; }

        public JsValue<bool> ShowInlineErrors { get; set; }

        public JsValue<string> FixedInlineErrorText { get; set; }

        public JsValue<string> Type { get; set; }
        public JsValue<int?> Id { get; set; }
        public JsValue<string> RequestExtraJsonData { get; set; }

        public JsValidatorOptions()
        {
            Renderer = () =>
            {
                return new JsOptionsBuilder(false)
                {
                    {"prefix", Prefix.TryCC(a=>a.ToJS())},
                    {"parentDiv", ParentDiv.TryCC(a=>a.ToJS())},
                    {"controllerUrl", ControllerUrl.TryCC(a=>a.ToJS())},
                    {"prefixToIgnore", PrefixToIgnore.TryCC(a=>a.ToJS())},
                    {"showInlineErrors",  ShowInlineErrors.TryCC(a=>a.ToJS())},
                    {"fixedInlineErrorText", FixedInlineErrorText.TryCC(a=>a.ToJS())},
                    {"type", Type.TryCC(a=>a.ToJS())},
                    {"id", Id.TryCC(a=>a.ToJS())},
                    {"requestExtraJsonData", RequestExtraJsonData.TryCC(a=>a.ToJS())},
                }.ToJS(); 
            };
        }
    }

    public static class JsValidator
    {
        public static JsInstruction ValidatePartial(JsValidatorOptions options)
        {
            return new JsInstruction(() => "ValidatePartial({0})".Formato(options.ToJS()));
        }

        public static JsInstruction TrySavePartial(JsValidatorOptions options)
        {
            return new JsInstruction(() => "TrySavePartial({0})".Formato(options.ToJS()));
        }

        public static JsInstruction Validate(JsValidatorOptions options)
        {
            return new JsInstruction(() => "Validate({0})".Formato(options.ToJS()));
        }

        public static JsInstruction TrySave(JsValidatorOptions options)
        {
            return new JsInstruction(() => "TrySave({0})".Formato(options.ToJS()));
        }

        public static JsInstruction EntityIsValid(JsValue<string> prefix, JsFunction onSuccess)
        {
            return EntityIsValid(new JsValidatorOptions { Prefix = prefix }, onSuccess);
        }

        public static JsInstruction EntityIsValid(JsValidatorOptions options, JsFunction onSuccess)
        {
            return new JsInstruction(() => "EntityIsValid({0},{1})".Formato(options.ToJS(), onSuccess.ToJS()));
        }
    }
}
