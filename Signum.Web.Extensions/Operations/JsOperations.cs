using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web.Extensions.Properties;

namespace Signum.Web.Operations
{
    public abstract class JsOperationBase : JsInstruction
    {
        protected JsOperationOptions options;

        public JsOperationBase(JsOperationOptions options)
        {
            this.options = options;
        }

        public JsInstruction DefaultSubmit()
        {
            return new JsInstruction(() => "{0}.defaultSubmit()".Formato(this.ToJS()));
        }

        public JsInstruction OperationSubmit()
        {
            return new JsInstruction(() => "{0}.operationSubmit()".Formato(this.ToJS()));
        }

        public JsInstruction OperationAjax(string newPrefix, JsFunction onSuccess)
        {
            return new JsInstruction(() => "{0}.operationAjax(\'{1}\', {2})".Formato(this.ToJS(), newPrefix, onSuccess.ToJS()));
        }

        public JsInstruction OperationAjax(string newPrefix, string querySelectedItems, JsFunction onSuccess)
        {
            return new JsInstruction(() => "{0}.operationAjax(\'{1}\',{2},{3})".Formato(this.ToJS(), newPrefix, querySelectedItems, onSuccess.ToJS()));
        }
    }
    
    public static class JsOpSuccess
    {
        public static readonly JsFunction DefaultDispatcher = new JsFunction() { Renderer = () => "SF.opOnSuccessDispatcher" };
        public static readonly JsFunction ReloadContent = new JsFunction() { Renderer = () => "SF.opReloadContent" };
        public static readonly JsFunction OpenPopup = new JsFunction() { Renderer = () => "SF.opOpenPopup" };
        public static readonly JsFunction OpenPopupNoDefaultOk = new JsFunction() { Renderer = () => "SF.opOpenPopupNoDefaultOk" };
        public static readonly JsFunction Navigate = new JsFunction() { Renderer = () => "SF.opNavigate" };
        public static readonly JsFunction DefaultContextualDispatcher = new JsFunction() { Renderer = () => "SF.opContextualOnSuccess" };
        public static readonly JsFunction MarkCellOnSuccess = new JsFunction() { Renderer = () => "SF.opMarkCellOnSuccess" };
    }

    public class JsOperationExecutor : JsOperationBase
    {
        public JsOperationExecutor(JsOperationOptions options)
            : base(options)
        {
            Renderer = () => "new SF.OperationExecutor(" + this.options.ToJS() + ")";
        }

        public JsInstruction DefaultExecute()
        {
            return new JsInstruction(() => "{0}.defaultExecute()".Formato(this.ToJS()));
        }

        public JsInstruction ContextualExecute(IdentifiableEntity entity, string operationName)
        {
            return new JsInstruction(() => Js.Confirm(
                Resources.PleaseConfirmYouDLikeToExecuteTheOperation0ToTheEntity123.Formato(operationName, entity.ToStr, entity.GetType().NiceName(), entity.Id),
                "{0}.contextualExecute()".Formato(this.ToJS())).ToJS());
        }
    }

    public class JsOperationConstructorFrom : JsOperationBase
    {
        public JsOperationConstructorFrom(JsOperationOptions options)
            : base(options)
        {
            Renderer = () => "new SF.ConstructorFrom(" + this.options.ToJS() + ")";
        }

        public JsInstruction DefaultConstruct()
        {
            return new JsInstruction(() => "{0}.defaultConstruct()".Formato(this.ToJS()));
        }

        public JsInstruction ContextualConstruct(IdentifiableEntity entity, string operationName)
        {
            return new JsInstruction(() => Js.Confirm(
                Resources.PleaseConfirmYouDLikeToExecuteTheOperation0ToTheEntity123.Formato(operationName, entity.ToStr, entity.GetType().NiceName(), entity.Id),
                "{0}.contextualConstruct()".Formato(this.ToJS())).ToJS());
        }
    }

    public class JsOperationConstructorFromMany : JsOperationBase
    {
        public JsOperationConstructorFromMany(JsOperationOptions options)
            : base(options)
        {
            Renderer = () => "new SF.ConstructorFromMany(" + this.options.ToJS() + ")";
        }

        public JsInstruction DefaultConstruct()
        {
            return new JsInstruction(() => "{0}.defaultConstruct()".Formato(this.ToJS()));
        }
    }

    public class JsOperationDelete : JsOperationBase
    {
        public JsOperationDelete(JsOperationOptions options)
            : base(options)
        {
            Renderer = () => "new SF.DeleteExecutor(" + this.options.ToJS() + ")";
        }

        public JsInstruction DefaultDelete(IdentifiableEntity entity)
        {
            return new JsInstruction(() => Js.Confirm(
                Resources.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem + ": {0} ({1}-{2})".Formato(entity.ToStr, entity.GetType().NiceName(), entity.Id), 
                "{0}.defaultDelete()".Formato(this.ToJS())).ToJS());
        }

        public JsInstruction ContextualDelete(IdentifiableEntity entity)
        {
            return new JsInstruction(() => Js.Confirm(
                Resources.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem + ": {0} ({1}-{2})".Formato(entity.ToStr, entity.GetType().NiceName(), entity.Id),
                "{0}.contextualDelete()".Formato(this.ToJS())).ToJS());
        }
    }
}
