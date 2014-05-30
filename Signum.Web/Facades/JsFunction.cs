using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Web
{
    /// <summary>
    /// Represents a Javascript/Typescript file that needs to be called using Require.js 
    /// 
    /// In order to create a new JsFunction just call the JsModule indexer and invoke the returning delegate.
    /// 
    /// JsModule MyModule = new JsModule("moduleName");
    /// MyModule["functionName"](arguments...)
    /// 
    /// Will translate to:
    /// 
    /// require(["moduleName"], function(mod) { mod.functionName(arguments...); }
    /// </summary>
    public class JsModule
    {
        public static JsModule Entities = new JsModule("Framework/Signum.Web/Signum/Scripts/Entities");
        public static JsModule Navigator = new JsModule("Framework/Signum.Web/Signum/Scripts/Navigator");
        public static JsModule Finder = new JsModule("Framework/Signum.Web/Signum/Scripts/Finder");
        public static JsModule Validator = new JsModule("Framework/Signum.Web/Signum/Scripts/Validator");
        public static JsModule Lines = new JsModule("Framework/Signum.Web/Signum/Scripts/Lines");
        public static JsModule Operations = new JsModule("Framework/Signum.Web/Signum/Scripts/Operations");

        public string Name {get;  private set;}

        /// <summary>
        /// File name of the Javascript / Typescript module as expected by Require.js
        /// </summary>
        public JsModule(string moduleName)
        {
            this.Name = Name;
        }

        public JsFunctionConstructor this[string functionName]
        {
            get
            {
                return args => new JsFunction(this, functionName, args);
            }
        }
    }

    public delegate JsFunction JsFunctionConstructor(params object[] args);  

    /// <summary>
    /// Represents a call to a Javascript/Typescript file using Require.js
    /// 
    /// In order to create a new JsFunction just call the JsModule indexer and invoke the returning delegate.
    /// 
    /// JsModule MyModule = new JsModule("moduleName");
    /// MyModule["functionName"](arguments...)
    /// 
    /// Will translate to:
    /// 
    /// require(["moduleName"], function(mod) { mod.functionName(arguments...); }
    /// </summary>
    public sealed class JsFunction : IHtmlString
    {
        public JsModule Module { get; set; }
        public string FunctionName { get; set; }
        public object[] Arguments { get; set; }
        public JsonSerializerSettings JsonSerializerSettings { get; set; }

        internal JsFunction(JsModule module, string functionName, params object[] arguments)
        {
            if (module == null)
                throw new ArgumentNullException("module");

            if (functionName == null)
                throw new ArgumentNullException("functionName");

            this.Module = module;
            this.FunctionName = functionName;
            this.Arguments = arguments ?? new object[0];
        }

        public override string ToString()
        {
            var varName = VarName(Module);

            var arguments = this.Arguments.ToString(a => a == This ? "that" : JsonConvert.SerializeObject(a, JsonSerializerSettings), ", "); 

            var result = "require(['" + Module + "'], function(" + varName + ") { " + varName + "." + FunctionName + "(" + this.Arguments + "); });";

            if (!this.Arguments.Contains(This))
                return result;

            return "(function(that) { " + result + "})(this)";
        }

        internal static string VarName(JsModule module)
        {
            var result = module.Name.TryAfterLast(".") ?? module.Name;

            return result.TryAfterLast("/") ?? result;
        }

        public string ToHtmlString()
        {
            return this.ToString();
        }

        public static object This = new object();

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

    public static class JsExtensions
    {
        public static ChooserOption ToChooserOption(this Enum enumValue)
        {
            return new ChooserOption(enumValue.ToString(), enumValue.NiceToString());
        }

        public static ChooserOption ToChooserOption(this Symbol symbol)
        {
            return new ChooserOption(symbol.Key, symbol.NiceToString());
        }

        public static JArray ToJsTypeInfos(this Implementations implementations, bool isSearch)
        {
            if (implementations.IsByAll)
                return null;

            return new JArray(implementations.Types.Select(t => ToJsTypeInfo(t, isSearch)).ToArray());
        }

        public static JObject ToJsTypeInfo(this Type type, bool isSearch)
        {
            var result = new JObject()
            {
                {"name", Navigator.ResolveWebTypeName(type)},
                {"niceName", type.NiceName()},
                {"creable", Navigator.IsCreable(type, isSearch)},
                {"findable", Navigator.IsFindable(type)},
            };

            var preConstruct = Constructor.ClientManager.GetPreConstructorScript(type);

            if (preConstruct != null)
                result.Add("preConstruct", preConstruct);

            return result;
        }
    }
}