//#region usings
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Signum.Entities.Operations;
//using System.Web.Mvc;
//using Signum.Entities;
//using System.Web;
//using Signum.Utilities;
//using Signum.Entities.Basics;
//using Signum.Web.Extensions.Properties;
//#endregion

//namespace Signum.Web.Operations
//{
//    public static class OperationButtonFactory 
//    {
//        public static ToolBarButton Create(HtmlHelper helper, OperationInfo oi, OperationSettings os, string prefix)
//        {
//            return new ToolBarButton
//            {
//                Id = EnumDN.UniqueKey(oi.Key),
//                DivCssClass = os.TryCC(o=>o.CssClass),
//                AltText = os.TryCC(o=>o.AltText) ?? oi.CanExecute,
//                Text = os.TryCC(o=>o.Text) ?? oi.Key.NiceToString(),
//                OnClick =  prefix => os.TryCS(o => o.Post) == true ? 
//                    GetServerClickPost(helper.



//            }; 

//        }



//        public override string ToString(HtmlHelper helper, string prefix)
//        {
//            if (Text == null)
//                Text = OperationInfo.Key.NiceToString();
//            AltText = OperationInfo.TryCC(oi => oi.CanExecute) ?? Text ?? OperationInfo.Key.NiceToString();

//            if (Id == null)
//                Id = Settings.TryCC(set => set.Options).TryCC(opt => opt.OperationKey) ?? EnumDN.UniqueKey(OperationInfo.Key);

//            if (OnClick == null)
//                OnClick = prefix => GetServerClickAjax(helper.ViewContext.HttpContext, prefix) ??
//                          GetServerClickPost(helper.ViewContext.HttpContext);

//            Enabled = OperationInfo.CanExecute == null;

//            return base.ToString(helper, prefix);
//        }

//        private string GetServerClickAjax(HttpContextBase httpContext, OperationInfo oi, OperationSettings os,  string prefix)
//        {
//            if (oi.OperationType == OperationType.Execute)
//                return JsOperationBase.Execute(new JsOperationExecutor(CreateJsOperationOptions(httpContext, os, oi, prefix))).ToJS();
//            else if (oi.OperationType == OperationType.ConstructorFrom)
//                return JsOperationBase.ConstructFrom(new JsOperationConstructorFrom(CreateJsOperationOptions(httpContext, os, oi, prefix))).ToJS();
//            else if (oi.OperationType == OperationType.ConstructorFromMany)
//                return JsOperationBase.ConstructFromMany(new JsOperationConstructorFromMany(CreateJsOperationOptions(httpContext, os, oi, prefix))).ToJS();
//            else if (oi.OperationType == OperationType.Delete)
//                return JsOperationBase.Delete(new JsOperationDelete(CreateJsOperationOptions(httpContext, os, oi, prefix))).ToJS();

//            throw new InvalidOperationException(Resources.InvalidOperationType0inTheConstructionOfOperation1.Formato(oi.OperationType.ToString(), EnumDN.UniqueKey(OperationInfo.Key)));
//        }

//        private string GetServerClickPost(HttpContextBase httpContext, OperationInfo oi, OperationSettings os, string prefix)
//        {
//            if (oi.OperationType == OperationType.Execute)
//                return JsOperationBase.ExecutePost(new JsOperationExecutor(CreateJsOperationOptions(httpContext, os, oi, prefix))).ToJS();
//            else if (oi.OperationType == OperationType.ConstructorFrom)
//            {
//                JsOperationOptions options = CreateJsOperationOptions(httpContext);
//                options.ReturnType = os.TryCC(sett => sett.Options).TryCC(opt => opt.ReturnType) ?? OperationInfo.ReturnType;
//                return JsOperationBase.ConstructFromPost(new JsOperationConstructorFrom(options)).ToJS();
//            }
//            else if (oi.OperationType == OperationType.Delete)
//                return null;

//            throw new InvalidOperationException(Resources.InvalidOperationType0inTheConstructionOfOperation1.Formato(OperationInfo.OperationType.ToString(), EnumDN.UniqueKey(OperationInfo.Key)));
//        }

//        private JsOperationOptions CreateJsOperationOptions(HttpContextBase httpContext, OperationSettings os, OperationInfo oi, string prefix)
//        {
//            //JsOperationOptions settingOptions = Settings.TryCC(sett => sett.Options);



//            return new JsOperationOptions
//            {
//                OperationKey = EnumDN.UniqueKey(oi.Key),
//                IsLite = oi.Lite,
//                Prefix = prefix,
//                ControllerUrl = os.TryCC(s => s.ControllerUrl),
//                ValidationControllerUrl = os.TryCC(s => s.ValidationControllerUrl),
//                AvoidValidation = os.TryCS(opt => opt.AvoidValidation) ?? false,
//                ConfirmMessage = os.TryCC(opt => opt.ConfirmMessage),
//                OnCancelled = os.TryCC(opt => opt.OnCancelled),
//                OnOk = os.TryCC(opt => opt.OnOk),
//                AvoidDefaultOk = os.TryCS(opt => opt.AvoidDefaultOk) ?? false,
//                OnOperationSuccess = os.TryCC(opt => opt.OnOperationSuccess),
//                MultiStep = os.TryCS(opt => opt.MultiStep) ?? false,
//                NavigateOnSuccess = os.TryCS(opt => opt.NavigateOnSuccess) ?? false,
//                ClosePopupOnSuccess = os.TryCS(opt => opt.ClosePopupOnSuccess) ?? false,
//                RequestExtraJsonData = os.TryCC(opt => opt.RequestExtraJsonData),
//            };
//        }

//    }

//    public class OperationSettings
//    {
//        public string Text { get; set; }
//        public string AltText { get; set; }
//        public string CssClass { get; set; }

//        public string ControllerUrl { get; set; }
//        public string ValidationControllerUrl { get; set; }
//        public bool AvoidValidation { get; set; }
//        public bool AvoidDefaultOk { get; set; }

//        public string OnOk { get; set; }
//        public string OnOperationSuccess { get; set; }
//        public string OnCancelled { get; set; }
//        public bool MultiStep { get; set; }
//        public bool NavigateOnSuccess { get; set; }
//        public bool ClosePopupOnSuccess { get; set; }
//        public string ConfirmMessage { get; set; }
//        public string RequestExtraJsonData { get; set; }

//        public bool? Post { get; set; }
//    }

//    public class ConstructorSettings : OperationSettings
//    {
//        public Func<OperationInfo, IdentifiableEntity> Constructor { get; set; }
//        public Func<OperationInfo, bool> IsVisible { get; set; }
//    }

//    public class EntityOperationSettings : OperationSettings
//    {
//        public Func<IdentifiableEntity, bool> IsVisible { get; set; }
//    }

//    public class ConstructorFromManySettings : OperationSettings
//    {
//        public Func<ConstructorFromManyEventArgs, IdentifiableEntity> Constructor { get; set; }
//        public Func<object, OperationInfo, bool> IsVisible { get; set; }
//    }

//    public class ConstructorFromManyEventArgs : EventArgs
//    {
//        public object QueryName { get; internal set; }
//        public List<Lite> Entities { get; internal set; }
//        public OperationInfo OperationInfo { get; internal set; }
//    }

//    public class JsFunction : IEnumerable<JsRenderer>
//    {
//        List<JsRenderer> instructions = new List<JsRenderer>();

//        string[] args;
//        public JsFunction(params string[] args)
//        {
//            this.args = args;
//        }

//        public void Add(JsRenderer instruction)
//        {
//            instructions.Add(instruction);
//        }

//        public override string ToString()
//        {
//            return "function({0}){{{1}}}".Formato(args, instructions.ToString(a => a.ToJS(), ";\r\n"));
//        }
//    }
//}
