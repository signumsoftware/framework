using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web;
using Signum.Web.Controllers;

namespace Signum.Web
{
    public class JsValidatorOptions : JsRenderer
    {
        public JsValue<string> Prefix { get; set; }
        public JsValue<string> ControllerUrl { get; set; }
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
                    {"controllerUrl", ControllerUrl.TryCC(a=>a.ToJS())},
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
        public static JsInstruction NewPartialValidator(JsValidatorOptions options)
        {
            return new JsInstruction(() => "new SF.PartialValidator({0})".Formato(options.ToJS()));
        }

        public static JsInstruction NewValidator(JsValidatorOptions options)
        {
            return new JsInstruction(() => "new SF.Validator({0})".Formato(options.ToJS()));
        }

        public static JsInstruction ValidatePartial(JsValidatorOptions options)
        {
            if (options.ControllerUrl == null)
                options.ControllerUrl = RouteHelper.New().SignumAction("ValidatePartial");

            return new JsInstruction(() => "{0}.validate()".Formato(NewPartialValidator(options)));
        }

        public static JsInstruction TrySavePartial(JsValidatorOptions options)
        {
            if (options.ControllerUrl == null)
                options.ControllerUrl = RouteHelper.New().SignumAction("TrySavePartial");

            return new JsInstruction(() => "{0}.trySave()".Formato(NewPartialValidator(options)));
        }

        public static JsInstruction Validate(JsValidatorOptions options)
        {
            if (options.ControllerUrl == null)
                options.ControllerUrl = RouteHelper.New().SignumAction("Validate");

            return new JsInstruction(() => "{0}.validate()".Formato(NewValidator(options)));
        }

        public static JsInstruction TrySave(JsValidatorOptions options)
        {
            if (options.ControllerUrl == null)
                options.ControllerUrl = RouteHelper.New().SignumAction("TrySave");

            return new JsInstruction(() => "{0}.trySave()".Formato(NewValidator(options)));
        }

        public static JsInstruction EntityIsValid(JsValue<string> prefix, JsFunction onSuccess)
        {
            return EntityIsValid(new JsValidatorOptions { Prefix = prefix }, onSuccess);
        }

        public static JsInstruction EntityIsValid(JsValidatorOptions options, JsFunction onSuccess)
        {
            if (options.ControllerUrl == null)
                options.ControllerUrl = RouteHelper.New().SignumAction(options.Prefix == null ? "Validate" : "ValidatePartial");

            return new JsInstruction(() => "SF.EntityIsValid({0},{1})".Formato(options.ToJS(), onSuccess.ToJS()));
        }
    }
}
