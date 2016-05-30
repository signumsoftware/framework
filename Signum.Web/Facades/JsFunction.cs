using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Text.RegularExpressions;

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
        public JsModule(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(name);

            this.Name = name;
        }

        public JsFunctionConstructor this[string functionName]
        {
            get { return args => new JsFunction(this, functionName, args); }
        }

        public override string ToString()
        {
            return Name;
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

            var arguments = this.Arguments.ToString(a =>
                a == This ? "that" : 
                a == Event ? "e" :                
                JsonConvert.SerializeObject(a, JsonSerializerSettings), ", ");

            var result = "require(['" + Module + "'], function(" + varName + ") { " + varName + "." + FunctionName + "(" + arguments + "); });";

            if (!this.Arguments.Contains(This) && !this.Arguments.Contains(Event))
                return result;

            return "(function(that, e) { " + result + " })(this, event)";
        }

        internal static string VarName(JsModule module)
        {
            var result = module.Name.TryAfterLast(".") ?? module.Name;

            result = result.TryAfterLast("/") ?? result;

            result = Regex.Replace(result, "[^a-zA-Z0-9]", "");

            return result;
        }

        public string ToHtmlString()
        {
            return this.ToString();
        }

        public static object This = new object();
        public static object Event = new object();

        public MvcHtmlString ToScriptTag()
        {
            return MvcHtmlString.Create("<script>" + this.ToString() + "</script>");
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

        public static ChooserOption ToChooserOption(this Lite<IEntity> lite)
        {
            return new ChooserOption(lite.KeyLong(), lite.ToString());
        }

        public static JsTypeInfo[] ToJsTypeInfos(this Implementations implementations, bool isSearch, string prefix)
        {
            if (implementations.IsByAll)
                return null;

            return implementations.Types.Select(t => ToJsTypeInfo(t, isSearch, prefix)).ToArray();
        }

        public static JsTypeInfo ToJsTypeInfo(this Type type, bool isSearch, string prefix)
        { 
            var result = new JsTypeInfo()
            {
                name = Navigator.ResolveWebTypeName(type),
                niceName = type.NiceName(),
                creable = Navigator.IsCreable(type, isSearch),
                findable = Finder.IsFindable(type),
                preConstruct = new JRaw(Constructor.ClientManager.GetPreConstructorScript(new ClientConstructorContext(type, prefix))),
                avoidPopup = (Navigator.Manager.EntitySettings.TryGetC(type)?.AvoidPopup) ?? false,
            };

            return result;
        }

        public class JsTypeInfo
        {
            public string name;
            public string niceName;
            public bool creable;
            public JRaw preConstruct;
            public bool findable;
            public bool avoidPopup;
        }
    }
}