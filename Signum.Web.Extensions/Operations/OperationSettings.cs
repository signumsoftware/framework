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
        public string Text { get; set; }
        public string AltText { get; set; }
   

        public string ControllerUrl { get; set; }
        public string RequestExtraJsonData { get; set; }
    }


    public class ConstructorSettings : OperationSettings
    {
        public Func<ConstructorOperationContext, ViewResultBase> VisualConstructor { get; set; }
        public Func<ConstructorOperationContext, IdentifiableEntity> Constructor { get; set; }
        public Func<ConstructorOperationContext, bool> IsVisible { get; set; }
    }

    public class EntityOperationSettings : OperationSettings
    {
        static EntityOperationSettings()
        {
            CssClass = _ => null;
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

    public class QueryOperationSettings : OperationSettings
    {
        public Func<QueryOperationContext, bool> IsVisible { get; set; }
        public Func<QueryOperationContext, JsInstruction> OnClick { get; set; }
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
            return new JsOperationOptions
            {
                OperationKey = EnumDN.UniqueKey(OperationInfo.Key),
                IsLite = OperationInfo.Lite,
                Prefix = this.Prefix,
                ControllerUrl = OperationSettings.TryCC(s => s.ControllerUrl).TryCC(c => (JsValue<string>)c),
                RequestExtraJsonData = OperationSettings.TryCC(opt => opt.RequestExtraJsonData),
            };
        }
    }

    public class QueryOperationContext : OperationContext
    {
        public object QueryName { get; internal set; }
        public QueryOperationSettings OperationSettings { get; internal set; }

        public JsOperationOptions Options()
        {
            return new JsOperationOptions
            {
                OperationKey = EnumDN.UniqueKey(OperationInfo.Key),
                IsLite = OperationInfo.Lite,
                Prefix = this.Prefix,
                ControllerUrl = OperationSettings.TryCC(s => s.ControllerUrl).TryCC(c => (JsValue<string>)c),
                RequestExtraJsonData = OperationSettings.TryCC(opt => (JsInstruction)opt.RequestExtraJsonData), //Not quoted
            };
        }
    }
}
