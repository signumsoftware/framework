using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web.Properties;

namespace Signum.Web.Operations
{
    public abstract class JsOperationBase : JsInstruction
    {
        protected JsOperationOptions options;

        public JsOperationBase(JsOperationOptions options)
        {
            this.options = options;
        }

        public JsInstruction validateAndSubmit()
        {
            return new JsInstruction(() => "{0}.validateAndSubmit()".Formato(this.ToJS()));
        }

        public JsInstruction submit()
        {
            return new JsInstruction(() => "{0}.submit()".Formato(this.ToJS()));
        }

        public JsInstruction ajax(string newPrefix, JsFunction onSuccess)
        {
            return new JsInstruction(() => "{0}.ajax(\'{1}\', {2})".Formato(this.ToJS(), newPrefix, onSuccess.ToJS()));
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

        public JsInstruction validateAndAjax()
        {
            return new JsInstruction(() => "{0}.validateAndAjax()".Formato(this.ToJS()));
        }

        public JsInstruction validateAndAjax(string newPrefix, JsFunction onSuccess)
        {
            return new JsInstruction(() => "{0}.validateAndAjax(\'{1}\', {2})".Formato(this.ToJS(), newPrefix, onSuccess.ToJS()));
        }

        public JsInstruction ContextualExecute()
        {
            return new JsInstruction(() => "{0}.contextualExecute()".Formato(this.ToJS()));
        }
    }

    public class JsOperationConstructorFrom : JsOperationBase
    {
        public JsOperationConstructorFrom(JsOperationOptions options)
            : base(options)
        {
            Renderer = () => "new SF.ConstructorFrom(" + this.options.ToJS() + ")";
        }

        public JsInstruction validateAndAjax()
        {
            return new JsInstruction(() => "{0}.validateAndAjax()".Formato(this.ToJS()));
        }

        public JsInstruction validateAndAjax(string newPrefix, JsFunction onSuccess)
        {
            return new JsInstruction(() => "{0}.validateAndAjax(\'{1}\', {2})".Formato(this.ToJS(), newPrefix, onSuccess.ToJS()));
        }

        public JsInstruction ContextualConstruct()
        {
            return new JsInstruction(() => "{0}.contextualConstruct()".Formato(this.ToJS()));
        }
    }

    public class JsOperationConstructorFromMany : JsOperationBase
    {
        public JsOperationConstructorFromMany(JsOperationOptions options)
            : base(options)
        {
            Renderer = () => "new SF.ConstructorFromMany(" + this.options.ToJS() + ")";
        }

        public JsInstruction ajaxSelected()
        {
            return new JsInstruction(() => "{0}.ajaxSelected()".Formato(this.ToJS()));
        }

        public JsInstruction ajaxSelected(string newPrefix, JsFunction onSuccess)
        {
            return new JsInstruction(() => "{0}.ajaxSelected(\'{1}\',{2})".Formato(this.ToJS(), newPrefix, onSuccess.ToJS()));
        }

        public JsInstruction submitSelected()
        {
            return new JsInstruction(() => "{0}.submitSelected()".Formato(this.ToJS()));
        }
    }

    public class JsOperationDelete : JsOperationBase
    {
        public JsOperationDelete(JsOperationOptions options)
            : base(options)
        {
            Renderer = () => "new SF.DeleteExecutor(" + this.options.ToJS() + ")";
        }

        public JsInstruction confirmAndAjax(IdentifiableEntity entity)
        {
            return new JsInstruction(() => Js.Confirm(
                OperationMessage.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem.NiceToString() + ": {0} ({1}-{2})".Formato(entity.ToString(), entity.GetType().NiceName(), entity.Id),
                "{0}.ajax()".Formato(this.ToJS())).ToJS());
        }

        public JsInstruction ContextualDelete(List<Lite<IdentifiableEntity>> entities)
        {
            if (entities.Count == 1)
            {
                return new JsInstruction(() => Js.Confirm(
                    OperationMessage.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem.NiceToString() + ": {0} ({1}-{2})".Formato(entities[0].ToString(), entities[0].GetType().NiceName(), entities[0].Id),
                    "{0}.contextualDelete()".Formato(this.ToJS())).ToJS());
            }
            else
            {
                return new JsInstruction(() => Js.Confirm(
                    OperationMessage.PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem.NiceToString(),
                    "{0}.contextualDelete()".Formato(this.ToJS())).ToJS());
            }
        }
    }
}
