using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Web
{
    public class JsFunction : IHtmlString
    {
        public static string EntitiesModule = "Framework/Signum.Web/Signum/Scripts/Entities";
        public static string NavigatorModule = "Framework/Signum.Web/Signum/Scripts/Navigator";
        public static string FinderModule = "Framework/Signum.Web/Signum/Scripts/Finder";
        public static string ValidatorModule = "Framework/Signum.Web/Signum/Scripts/Validator";
        public static string LinesModule = "Framework/Signum.Web/Signum/Scripts/Lines";
        public static string OperationsModule = "Framework/Signum.Web/Signum/Scripts/Operations";

        public string Module { get; set; }
        public string FunctionName { get; set; }
        public string Arguments { get; set; }
        public JsonSerializerSettings JsonSerializerSettings { get; set; }

        /// <summary>
        /// require(["module"], function(mod) { mod.functionName(arguments...); }
        /// </summary>
        public JsFunction(string module, string functionName, params object[] arguments)
        {
            if (module == null)
                throw new ArgumentNullException("module");

            if (functionName == null)
                throw new ArgumentNullException("functionName");

            this.Module = module;
            this.FunctionName = functionName;
            this.Arguments = arguments.EmptyIfNull().ToString(a => JsonConvert.SerializeObject(a, JsonSerializerSettings), ", ");
        }

        public override string ToString()
        {
            var varName = VarName(Module);

            return "require(['" + Module + "'], function(" + varName + ") { " + varName + "." + FunctionName + "(" + this.Arguments + "); });";
        }

        protected static string VarName(string module)
        {
            var result = module.TryAfterLast(".") ?? module;

            return result.TryAfterLast("/") ?? result;
        }

        public string ToHtmlString()
        {
            return this.ToString();
        }

        public static JsLiteral Literal(string jsText)
        {
            return new JsLiteral(jsText);
        }

        [JsonConverter(typeof(JsLiteralConverter))]
        public class JsLiteral
        {
            public string JsText { get; private set; }

            public JsLiteral(string jsText)
            {
                this.JsText = jsText;
            }
        }

        class JsLiteralConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(JsLiteral) == objectType;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteRaw(((JsLiteral)value).JsText);
            }
        }

        public static string SFControlThen(string prefix, string functionCall)
        {
            return "$('#" + prefix + "').SFControl().then(function(c){c." + functionCall + ";})";
        }
    }

    public class JsFunctionSender : JsFunction
    { 
        /// <summary>
        /// (function(that){ require("module", function(mod) { mod.functionName(that, arguments...); })})(this);
        /// </summary>
        public JsFunctionSender(string module, string functionName, params object[] arguments) 
            : base(module, functionName, arguments)
        {
        }

        public override string ToString()
        {
            var varName = VarName(Module);

            var args = this.Arguments.HasText() ? (", " + this.Arguments.Replace("\"", "'")) : "";

            return "(function(that) { require(['" + Module + "'], function(" + varName + ") { " + varName + "." + FunctionName + "(that" + args + "); }); })(this);";
        }
    }

    public class ChooserOption
    {
        public ChooserOption(string value, string toStr)
        {
            this.value = value;
            this.toStr = toStr;
        }

        public string value;
        public string toStr; 
    }

    public static class ChooserOptionExtensions
    {
        public static ChooserOption ToChooserOption(this Type type)
        {
            return new ChooserOption(Navigator.ResolveWebTypeName(type), type.NiceName()); 
        }

        public static ChooserOption ToChooserOptionToSting(this Enum enumValue)
        {
            return new ChooserOption(enumValue.ToString(), enumValue.NiceToString());
        }

        public static ChooserOption ToChooserOptionMultiEnum(this Enum enumValue)
        {
            return new ChooserOption(MultiEnumDN.UniqueKey(enumValue), enumValue.NiceToString());
        }
    }
}