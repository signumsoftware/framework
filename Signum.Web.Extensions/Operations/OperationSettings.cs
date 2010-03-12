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
    public class OperationButton : ToolBarButton
    {
        public OperationInfo OperationInfo { get; set; }
        public OperationSettings Settings { get; set; }

        public override string ToString()
        {
            return base.ToString();
        }

        public override string ToString(HtmlHelper helper, string prefix)
        {
            if (Text == null)
                Text = OperationInfo.Key.NiceToString();
            AltText = OperationInfo.TryCC(oi => oi.CanExecute) ?? Text ?? OperationInfo.Key.NiceToString();

            if (Id == null)
                Id = Settings.TryCC(set => set.Options).TryCC(opt => opt.OperationKey) ?? EnumDN.UniqueKey(OperationInfo.Key);
            
            if (OnClick == null)
                OnClick = GetServerClickAjax(helper.ViewContext.HttpContext) ??
                          GetServerClickPost(helper.ViewContext.HttpContext);

            Enabled = OperationInfo.CanExecute == null;

            return base.ToString(helper, prefix);
        }

        private string GetServerClickAjax(HttpContextBase httpContext)
        {
            if (Settings.TryCS(set => set.Post) == true)
                return null;

            if (OperationInfo.OperationType == OperationType.Execute)
                return JsOperationBase.Execute(new JsOperationExecutor(CreateJsOperationOptions(httpContext))).ToJS();
            else if (OperationInfo.OperationType == OperationType.ConstructorFrom)
                return JsOperationBase.ConstructFrom(new JsOperationConstructorFrom(CreateJsOperationOptions(httpContext))).ToJS();
            else if (OperationInfo.OperationType == OperationType.Delete)
                return JsOperationBase.Delete(new JsOperationDelete(CreateJsOperationOptions(httpContext))).ToJS();

            throw new InvalidOperationException(Resources.InvalidOperationType0inTheConstructionOfOperation1.Formato(OperationInfo.OperationType.ToString(), EnumDN.UniqueKey(OperationInfo.Key)));
        }

        protected internal virtual string GetServerClickPost(HttpContextBase httpContext)
        {
            if (Settings == null || Settings.Post == null || Settings.Post == false)
                return null;

            if (OperationInfo.OperationType == OperationType.Execute)
                return JsOperationBase.ExecutePost(new JsOperationExecutor(CreateJsOperationOptions(httpContext))).ToJS();
            else if (OperationInfo.OperationType == OperationType.ConstructorFrom)
            {
                JsOperationOptions options = CreateJsOperationOptions(httpContext);
                options.ReturnType = Settings.TryCC(sett => sett.Options).TryCC(opt => opt.ReturnType) ?? OperationInfo.ReturnType;
                return JsOperationBase.ConstructFromPost(new JsOperationConstructorFrom(options)).ToJS();
            }
            else if (OperationInfo.OperationType == OperationType.Delete)
                return null;

            throw new InvalidOperationException(Resources.InvalidOperationType0inTheConstructionOfOperation1.Formato(OperationInfo.OperationType.ToString(), EnumDN.UniqueKey(OperationInfo.Key)));
        }

        private JsOperationOptions CreateJsOperationOptions(HttpContextBase httpContext)
        {
            JsOperationOptions settingOptions = Settings.TryCC(sett => sett.Options);

            return new JsOperationOptions
            {
                OperationKey = settingOptions.TryCC(opt => opt.OperationKey) ?? EnumDN.UniqueKey(OperationInfo.Key),
                IsLite = settingOptions.TryCS(opt => opt.IsLite) ?? OperationInfo.Lite,
                Prefix = settingOptions.TryCC(opt => opt.Prefix) ?? (httpContext.Request.IsAjaxRequest() ? (httpContext.Request.Params["prefix"] ?? "") : ""),
                ControllerUrl = settingOptions.TryCC(opt => opt.ControllerUrl),
                ValidationControllerUrl = settingOptions.TryCC(opt => opt.ValidationControllerUrl),
                AvoidValidation = settingOptions.TryCS(opt => opt.AvoidValidation),
                ConfirmMessage = settingOptions.TryCC(opt => opt.ConfirmMessage),
                OnCancelled = settingOptions.TryCC(opt => opt.OnCancelled),
                OnOk = settingOptions.TryCC(opt => opt.OnOk),
                AvoidDefaultOk = settingOptions.TryCS(opt => opt.AvoidDefaultOk) ?? false,
                OnOperationSuccess = settingOptions.TryCC(opt => opt.OnOperationSuccess),
                MultiStep = settingOptions.TryCS(opt => opt.MultiStep),
                NavigateOnSuccess = settingOptions.TryCS(opt => opt.NavigateOnSuccess),
                ClosePopupOnSuccess = settingOptions.TryCS(opt => opt.ClosePopupOnSuccess),
                RequestExtraJsonData = settingOptions.TryCC(opt => opt.RequestExtraJsonData),
            };
        }

    }

    public abstract class OperationSettings
    {
        JsOperationOptions options = new JsOperationOptions();
        public JsOperationOptions Options
        {
            get { return options; }
            set { options = value; }
        }

        public bool? Post { get; set; }
    }

    public class ConstructorSettings : OperationSettings
    {
        public Func<OperationInfo, HttpContextBase, IdentifiableEntity> Constructor { get; set; }
        public Func<OperationInfo, bool> IsVisible { get; set; }
    }

    public class EntityOperationSettings : OperationSettings
    {
        public Func<IdentifiableEntity, bool> IsVisible { get; set; }
    }

    public class ConstructorFromManySettings : OperationSettings
    {
        public Func<ConstructorFromManyEventArgs, HttpContextBase, IdentifiableEntity> Constructor { get; set; }
        public Func<object, OperationInfo, bool> IsVisible { get; set; }
    }

    public class ConstructorFromManyEventArgs : EventArgs
    {
        public object QueryName { get; internal set; }
        public List<Lite> Entities { get; internal set; }
        public OperationInfo OperationInfo { get; internal set; }
    }
}
