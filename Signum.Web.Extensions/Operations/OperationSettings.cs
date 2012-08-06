#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Operations;
using System.Web.Mvc;
using Signum.Entities;
using System.Web;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Web.Extensions.Properties;
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
        public string AltText { get; set; }
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

    public class EntityOperationSettings : OperationSettings
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
        public bool IsVisibleOnOkPopup { get; set; }
        public Func<EntityOperationContext, JsInstruction> OnClick { get; set; }
        public Func<ContextualOperationContext, bool> IsContextualVisible { get; set; }
        public Func<ContextualOperationContext, JsInstruction> OnContextualClick { get; set; }
    }

    public class QueryOperationSettings : OperationSettings
    {
        public QueryOperationSettings(Enum operationKey)
            : base(operationKey)
        {
        }


        public Func<QueryOperationContext, bool> IsVisible { get; set; }
        public Func<QueryOperationContext, JsInstruction> OnClick { get; set; }

        bool groupInMenu = true;
        /// <summary>
        /// Set to false if this operation is not to be grouped in a Constructors menu
        /// </summary>
        public bool GroupInMenu
        {
            get { return groupInMenu; }
            set { groupInMenu = value; }
        }
    }

    public abstract class OperationContext
    {
        public string Prefix { get; internal set; }
        public OperationInfo OperationInfo { get; internal set; }
    }

    public class ConstructorOperationContext : OperationContext
    {
        public VisualConstructStyle PreferredStyle { get; internal set; }
        public ControllerBase Controller { get; internal set; }
    }

    public class EntityOperationContext : OperationContext
    {
        public string PartialViewName { get; internal set; }
        public IdentifiableEntity Entity { get; internal set; }
        public EntityOperationSettings OperationSettings { get; internal set; }



        public JsOperationOptions Options()
        {
            return Options(null);
        }

        public JsOperationOptions Options(string actionName, string controllerName)
        {
            return Options(RouteHelper.New().Action(actionName,controllerName));
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
    }

    public class ContextualOperationContext : OperationContext
    { 
        public IdentifiableEntity Entity { get; internal set; }
        public object QueryName { get; internal set; }
        public EntityOperationSettings OperationSettings { get; internal set; }

        public JsOperationOptions Options()
        {
            return Options(null);
        }

        public JsOperationOptions Options(string actionName, string controllerName)
        {
            return Options(RouteHelper.New().Action(actionName, controllerName));
        }

        public JsOperationOptions Options(string controllerUrl)
        {
            return new JsOperationOptions
            {
                Operation = OperationInfo.Key,
                IsLite = OperationInfo.Lite,
                Prefix = this.Prefix,
                IsContextual = true,
                ControllerUrl = (JsValue<string>)controllerUrl,
                RequestExtraJsonData = OperationSettings.TryCC(opt => opt.RequestExtraJsonData) ?? 
                    "{{{0}:'{1}'}}".Formato(
                        TypeContextUtilities.Compose(Prefix, EntityBaseKeys.RuntimeInfo), 
                        "{0};{1};{2}".Formato(Navigator.ResolveWebTypeName(Entity.GetType()), Entity.Id, "o"))
            };
        }
    }

    public class QueryOperationContext : OperationContext
    {
        public object QueryName { get; internal set; }
        public QueryOperationSettings OperationSettings { get; internal set; }

        public JsOperationOptions Options()
        {
            return Options(null);
        }

        public JsOperationOptions Options(string actionName, string controllerName)
        {
            return Options(RouteHelper.New().Action(actionName, controllerName));
        }

        public JsOperationOptions Options(string controllerUrl)
        {
            return new JsOperationOptions
            {
                Operation = OperationInfo.Key,
                IsLite = OperationInfo.Lite,
                Prefix = this.Prefix,
                ControllerUrl = (JsValue<string>)controllerUrl,
                RequestExtraJsonData = OperationSettings.TryCC(opt => (JsInstruction)opt.RequestExtraJsonData), //Not quoted
            };
        }
    }
}
