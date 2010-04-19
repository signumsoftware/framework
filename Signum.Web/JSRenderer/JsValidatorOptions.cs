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
        public string Prefix { get; set; }
        public string ParentDiv { get; set; }
        public string ControllerUrl { get; set; }
        public string PrefixToIgnore { get; set; }

        public bool showInlineErrors = true;
        public bool ShowInlineErrors { get { return showInlineErrors; } set { showInlineErrors = value; } }

        public string fixedInlineErrorText = "*";
        public string FixedInlineErrorText { get { return fixedInlineErrorText; } set { fixedInlineErrorText = value; } }

        public string OnSuccess { get; set; }
        
        public string Type { get; set; }
        public int? Id { get; set; }
        public string RequestExtraJsonData { get; set; }

        public JsValidatorOptions()
        {
            renderer = () =>
            {
                return new JsOptionsBuilder(false)
                {
                    {"prefix", Prefix.TrySingleQuote()},
                    {"parentDiv", ParentDiv.TrySingleQuote()},
                    {"controllerUrl", ControllerUrl.TrySingleQuote()},
                    {"prefixToIgnore", PrefixToIgnore.TrySingleQuote()},
                    {"showInlineErrors", ShowInlineErrors? null: "false"},
                    {"fixedInlineErrorText", FixedInlineErrorText != "*" ? FixedInlineErrorText.TrySingleQuote() : null},
                    {"onSuccess", OnSuccess},
                    {"type",Type.TrySingleQuote()},
                    {"id",Id.TryToString()},
                    {"requestExtraJsonData",RequestExtraJsonData},
                }.ToJS(); 
            };
        }
    }

    public static class JsValidator
    {
        public static JsRenderer ValidatePartial(JsValidatorOptions options)
        {
            return new JsRenderer(() => "ValidatePartial({0})".Formato(options.ToJS()));
        }

        public static JsRenderer TrySavePartial(JsValidatorOptions options)
        {
            return new JsRenderer(() => "TrySavePartial({0})".Formato(options.ToJS()));
        }

        public static JsRenderer Validate(JsValidatorOptions options)
        {
            return new JsRenderer(() => "Validate({0})".Formato(options.ToJS()));
        }

        public static JsRenderer TrySave(JsValidatorOptions options)
        {
            return new JsRenderer(() => "TrySave({0})".Formato(options.ToJS()));
        }
    }
}
