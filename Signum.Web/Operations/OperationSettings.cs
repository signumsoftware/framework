#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Entities;
using System.Web;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
#endregion

namespace Signum.Web.Operations
{
    public abstract class OperationSettings
    {
        public OperationSettings(IOperationSymbolContainer symbol)
        {
            this.OperationSymbol = symbol.Operation; 
        }

        public OperationSymbol OperationSymbol { get; private set; }

        public string Text { get; set; }
    }

    public class ConstructorSettings : OperationSettings
    {
        public ConstructorSettings(IOperationSymbolContainer symbol)
            : base(symbol)
        {
        }

        public Func<ConstructorOperationContext, ViewResultBase> VisualConstructor { get; set; }
        public Func<ConstructorOperationContext, IdentifiableEntity> Constructor { get; set; }
    }

    public class EntityOperationGroup
    {
        public static readonly EntityOperationGroup None = new EntityOperationGroup();

        public static EntityOperationGroup Create = new EntityOperationGroup
        {
            Description = () => OperationMessage.Create.NiceToString(),
            SimplifyName = cs => Regex.Replace(cs, OperationMessage.CreateFromRegex.NiceToString(), m => m.Groups[1].Value.FirstUpper(), RegexOptions.IgnoreCase),
            CssClass = "sf-operation"
        };

        public Func<string> Description;
        public Func<string, string> SimplifyName;
        public string CssClass;
        public double Order = 100;
    }

    public class EntityOperationSettings : OperationSettings
    {
        public ContextualOperationSettings ContextualFromMany { get; private set; }
        public ContextualOperationSettings Contextual { get; private set; }
        public double Order { get; set; }

        public EntityOperationSettings(IOperationSymbolContainer symbol)
            : base(symbol)
        {
            this.Contextual = new ContextualOperationSettings(symbol);
            this.ContextualFromMany = new ContextualOperationSettings(symbol); 
        }

        static EntityOperationSettings()
        {
            Style = oi => oi.OperationType == OperationType.Delete ? BootstrapStyle.Danger :
                oi.OperationType == OperationType.Execute && oi.OperationSymbol.Key.EndsWith(".Save") ? BootstrapStyle.Primary :
                BootstrapStyle.Default;
        }

        public static Func<OperationInfo, BootstrapStyle> Style { get; set; }

        public EntityOperationGroup Group { get; set; }

        public Func<EntityOperationContext, string> ConfirmMessage { get; set; }
        public Func<EntityOperationContext, bool> IsVisible { get; set; }
        public Func<EntityOperationContext, JsOperationFunction> OnClick { get; set; }
    }

    public class ContextualOperationSettings : OperationSettings
    {
        public ContextualOperationSettings(IOperationSymbolContainer symbol)
            : base(symbol)
        {
        }

        public double Order { get; set; }
        public Func<ContextualOperationContext, string> ConfirmMessage { get; set; }
        public Func<ContextualOperationContext, bool> IsVisible { get; set; }
        public Func<ContextualOperationContext, JsOperationFunction> OnClick { get; set; }

    }

    public abstract class OperationContext
    {
        public string Prefix { get; set; }
        public OperationInfo OperationInfo { get; set; }
        public UrlHelper Url { get; set; }
    }

    public class ConstructorOperationContext : OperationContext
    {
        public VisualConstructStyle PreferredStyle { get; set; }
        public ControllerBase Controller { get; set; }
    }

    public class EntityOperationContext : OperationContext
    {
        public string PartialViewName { get; set; }
        public IdentifiableEntity Entity { get; internal set; }
        public EntityOperationSettings OperationSettings { get; internal set; }
        public string CanExecute { get; internal set; }
        public ViewMode ViewButtons { get; internal set; }
        public bool ShowOperations { get; set; }

        public JsOperationOptions Options()
        {
            var result = new JsOperationOptions(OperationInfo.OperationSymbol, this.Prefix) { isLite = OperationInfo.Lite };

            result.confirmMessage = OperationSettings != null && OperationSettings.ConfirmMessage != null ? OperationSettings.ConfirmMessage(this) :
                OperationInfo.OperationType == OperationType.Delete ? OperationMessage.PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem.NiceToString() : null;

            return result;
        }

        public override string ToString()
        {
            return OperationInfo.ToString();
        }


        public string Compose(string prefixPart)
        {
            return TypeContextUtilities.Compose(this.Prefix, prefixPart); 
        }
    }

    public class ContextualOperationContext : OperationContext
    {
        public List<Lite<IdentifiableEntity>> Entities { get; set; }
        public object QueryName { get; set; }
        public ContextualOperationSettings OperationSettings { get; set; }
        public string CanExecute { get; set; }

        public JsOperationOptions Options()
        {
            var result = new JsOperationOptions(OperationInfo.OperationSymbol, this.Prefix){ isLite = OperationInfo.Lite};

            result.confirmMessage = OperationSettings != null && OperationSettings.ConfirmMessage != null ? OperationSettings.ConfirmMessage(this) :
                OperationInfo.OperationType == OperationType.Delete ? OperationMessage.PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem.NiceToString() : null;

            return result;
        }
    }

    public class JsOperationOptions
    {
        public JsOperationOptions(OperationSymbol operation, string prefix)
        {
            this.operationKey = operation.Key;
            this.prefix = prefix;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string operationKey;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string prefix;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? isLite;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string confirmMessage;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string controllerUrl; 
    }

    public class JsOperationFunction : JsFunction
    {
        /// <summary>
        /// require("module", function(mod) { mod.functionName(operationOptions, arguments...); }
        /// </summary>
        public JsOperationFunction(string module, string functionName, params object[] arguments) :
            base(module, functionName, arguments)
        {
        }

        JsOperationOptions operationOptions;
        internal JsOperationFunction SetOptions(JsOperationOptions operationOptions)
        {
            this.operationOptions = operationOptions;
            return this;
        }

        public override string ToString()
        {
            if (operationOptions == null)
                throw new InvalidOperationException("Attempt to call JsOperationFunction.ToString without JsOperationFunction. Consider using JsFunction instead.");

            var varName = VarName(Module);

            var options = JsonConvert.SerializeObject(this.operationOptions, JsonSerializerSettings);

            var args = string.IsNullOrEmpty(Arguments) ? null : (", " + Arguments);

            return "require(['" + Module + "'], function(" + varName + ") { " + varName + "." + FunctionName + "(" + options + args + "); });";
        }
    }
}
