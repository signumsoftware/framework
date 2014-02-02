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
        public object[] Arguments { get; set; }
        public JsonSerializerSettings JsonSerializerSettings { get; set; }

        /// <summary>
        /// require("module", function(mod) { mod.functionName(arguments...); }
        /// </summary>
        public JsFunction(string module, string functionName, params object[] arguments)
        {
            if (module == null)
                throw new ArgumentNullException("module");

            if (functionName == null)
                throw new ArgumentNullException("functionName");

            this.Module = module;
            this.FunctionName = FunctionName;
            this.Arguments = arguments;
        }

        public override string ToString()
        {
            var varName = VarName(Module);

            var args = Arguments.EmptyIfNull().ToString(a => JsonConvert.SerializeObject(a, JsonSerializerSettings), ", ");

            return "require('" + Module + "', function(" + varName + ") { " + varName + "." + FunctionName + "(" + args + "); }";
        }

        protected static string VarName(string module)
        {
            var result = module.AfterLast(".") ?? module;

            return result.AfterLast("/") ?? result;
        }

        public string ToHtmlString()
        {
            return this.ToString();
        }
    }

    public class ChooserOption
    {
        public string value;
        public string toStr; 
    }

    public static class ChooserOptionExtensions
    {
        public static ChooserOption ToChooserOption(this Type type)
        {
            return new ChooserOption { value = Navigator.ResolveWebTypeName(type), toStr = type.NiceName() }; 
        }

        public static ChooserOption ToChooserOption(this Enum enumValue)
        {
            return new ChooserOption { value = enumValue.ToString(), toStr = enumValue.NiceToString() };
        }

        public static ChooserOption ToChooserOptionMultiEnum(this Enum enumValue)
        {
            return new ChooserOption { value = MultiEnumDN.UniqueKey(enumValue), toStr = enumValue.NiceToString() };
        }
    }
}