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
        public OperationSettings(Enum key)
        {
            this.Key = key; 
        }

        public Enum Key { get; private set; }

        public string Text { get; set; }
    }

    public class ConstructorSettings : OperationSettings
    {
        public ConstructorSettings(Enum operationKey)
            : base(operationKey)
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

        public EntityOperationSettings(Enum operationKey)
            : base(operationKey)
        {
            this.Contextual = new ContextualOperationSettings(operationKey);
            this.ContextualFromMany = new ContextualOperationSettings(operationKey); 
        }

        static EntityOperationSettings()
        {
            CssClass = _ => "sf-operation";
        }

        public static Func<Enum, string> CssClass { get; set; }

        public EntityOperationGroup Group { get; set; }

        public Func<EntityOperationContext, string> ConfirmMessage { get; set; }
        public Func<EntityOperationContext, bool> IsVisible { get; set; }
        public Func<EntityOperationContext, JsOperationFunction> OnClick { get; set; }
    }

    public class ContextualOperationSettings : OperationSettings
    {
        public ContextualOperationSettings(Enum operationKey)
            : base(operationKey)
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

        public JObject Options()
        {
            var result = new JObject()
            { 
                { "operationKey", OperationDN.UniqueKey(OperationInfo.Key) },
                { "isLite", OperationInfo.Lite },
                { "prefix", this.Prefix },
            };

            var confirm = OperationSettings != null && OperationSettings.ConfirmMessage != null ? OperationSettings.ConfirmMessage(this) :
                OperationInfo.OperationType == OperationType.Delete ? OperationMessage.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem.NiceToString() : null;

            if (confirm != null)
                result.Add("confirmMessage", confirm);

            return result;
        }

        public override string ToString()
        {
            return OperationInfo.ToString();
        }

    }

    public class ContextualOperationContext : OperationContext
    {
        public List<Lite<IdentifiableEntity>> Entities { get; set; }
        public object QueryName { get; set; }
        public ContextualOperationSettings OperationSettings { get; set; }
        public string CanExecute { get; set; }

        public JObject Options()
        {
            var result = new JObject()
            { 
                { "operationKey", OperationDN.UniqueKey(OperationInfo.Key) },
                { "isLite", OperationInfo.Lite },
                { "prefix", this.Prefix },
            };

            var confirm = OperationSettings != null && OperationSettings.ConfirmMessage != null ? OperationSettings.ConfirmMessage(this) :
                OperationInfo.OperationType == OperationType.Delete ? OperationMessage.PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem.NiceToString() : null;
            
            if (confirm != null)
                result.Add("confirmMessage", confirm);

            return result;
        }
    }

    public class JsOperationFunction : JsFunction
    {
        /// <summary>
        /// require("module", function(mod) { mod.functionName(operationSettings, arguments...); }
        /// </summary>
        public JsOperationFunction(string module, string functionName, params object[] arguments) :
            base(module, functionName, arguments)
        {
        }

        JObject OperationObjects;
        internal JsOperationFunction SetOptions(JObject operationObjects)
        {
            this.OperationObjects = operationObjects;
            return this;
        }

        public override string ToString()
        {
            var varName = VarName(Module);

            var args = (Arguments.IsNullOrEmpty() ? null :
                (", " + Arguments.EmptyIfNull().ToString(a => JsonConvert.SerializeObject(a, JsonSerializerSettings), ", ")));

            return "require(['" + Module + "'], function(" + varName + ") { " + varName + "." + FunctionName + "(" + OperationObjects.ToString() + args + "); });";
        }
    }
}
