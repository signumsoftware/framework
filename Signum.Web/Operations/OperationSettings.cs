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
        public string RequestExtraJsonData { get; set; }
    }

    public class ConstructorSettings : OperationSettings
    {
        public ConstructorSettings(Enum operationKey)
            : base(operationKey)
        {
        }

        public Func<ConstructorOperationContext, ViewResultBase> VisualConstructor { get; set; }
        public Func<ConstructorOperationContext, IdentifiableEntity> Constructor { get; set; }
        public Func<ConstructorOperationContext, bool> IsVisible { get; set; }
    }

    public abstract class EntityOperationSettingsBase : OperationSettings
    {
        public ContextualOperationSettings ContextualFromMany { get; set; }
        public ContextualOperationSettings Contextual { get; set; }

        public EntityOperationSettingsBase(Enum key)
            : base(key)
        {
        }
    }

    public class EntityOperationSettings : EntityOperationSettingsBase
    {
        public EntityOperationSettings(Enum operationKey)
            : base(operationKey)
        {
        }

        static EntityOperationSettings()
        {
            CssClass = _ => "sf-operation";
        }

        public static Func<Enum, string> CssClass { get; set; }

        bool groupInMenu = true;
        /// <summary>
        /// Set to false if this operation is not to be grouped in a Constructors menu
        /// </summary>
        public bool GroupInMenu 
        {
            get { return groupInMenu; }
            set { groupInMenu = value; }
        }

        public Func<EntityOperationContext, bool> IsVisible { get; set; }
        public Func<EntityOperationContext, JsInstruction> OnClick { get; set; }
    }

    public class ContextualOperationSettings : OperationSettings
    {
        public ContextualOperationSettings(Enum operationKey)
            : base(operationKey)
        {
        }

        public Func<ContextualOperationContext, bool> IsVisible { get; set; }
        public Func<ContextualOperationContext, JsInstruction> OnClick { get; set; }

    }

    public abstract class OperationContext
    {
        public string Prefix { get; set; }
        public OperationInfo OperationInfo { get; set; }

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
        public ViewButtons ViewButtons { get; internal set; }
        public bool ShowOperations { get; set; }

        public JsOperationOptions Options()
        {
            return Options(null);
        }

        public JsOperationOptions Options(string actionName, string controllerName)
        {
            return Options(RouteHelper.New().Action(actionName,controllerName));
        }

        public JsOperationOptions Options<TController>(Expression<Action<TController>> action)
            where TController : Controller
        {
            return Options(RouteHelper.New().Action(action));
        }

        public JsOperationOptions Options(string controllerUrl)
        {
            return new JsOperationOptions
            {
                Operation = OperationInfo.Key,
                IsLite = OperationInfo.Lite,
                Prefix = this.Prefix,
                ControllerUrl = (JsValue<string>)controllerUrl,
                RequestExtraJsonData = OperationSettings.TryCC(opt => opt.RequestExtraJsonData),
            };
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

        public JsOperationOptions Options()
        {
            return Options(null);
        }

        public JsOperationOptions Options(string actionName, string controllerName)
        {
            return Options(RouteHelper.New().Action(actionName, controllerName));
        }

        public JsOperationOptions Options<TController>(Expression<Action<TController>> action)
            where TController : Controller
        {
            return Options(RouteHelper.New().Action(action));
        }

        public JsOperationOptions Options(string controllerUrl)
        {
            var requestData = OperationSettings.TryCC(opt => opt.RequestExtraJsonData);
            if (requestData == null)
            {
                if (Entities.Count == 1) 
                    requestData = "{{{0}:'{1}'}}".Formato(TypeContextUtilities.Compose(Prefix, EntityBaseKeys.RuntimeInfo), 
                        "{0};{1};{2}".Formato(Navigator.ResolveWebTypeName(Entities[0].EntityType), Entities[0].Id, "o"));
                else 
                    requestData = "{{lites:'{0}'}}".Formato(Entities.Select(e => e.Key()).ToString(",")); 
            }

            return new JsOperationOptions
            {
                Operation = OperationInfo.Key,
                IsLite = OperationInfo.Lite,
                Prefix = this.Prefix,
                IsContextual = true,
                ControllerUrl = (JsValue<string>)controllerUrl,
                RequestExtraJsonData = requestData
            };
        }
    }
}
