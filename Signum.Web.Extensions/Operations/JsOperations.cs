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
            return new JsInstruction(() => "{0}.operationAjax('{1}', {2})".Formato(this.ToJS(), newPrefix, onSuccess.ToJS()));
        }

        public JsInstruction OperationAjax(string newPrefix, string querySelectedItems, JsFunction onSuccess)
        {
            return new JsInstruction(() => "{0}.operationAjax('{1}',{2},{3})".Formato(this.ToJS(), newPrefix, querySelectedItems, onSuccess.ToJS()));
        }
    }


    public static class JsOp
    {
        public static readonly JsFunction ReloadContent = new JsFunction() { Renderer = () => "OpReloadContent" };
        public static readonly JsFunction OpenPopup = new JsFunction() { Renderer = () => "OpOpenPopup" };
        public static readonly JsFunction OpOpenPopupNoDefaultOk = new JsFunction() { Renderer = () => "OpOpenPopupNoDefaultOk" };
        public static readonly JsFunction Navigate = new JsFunction() { Renderer = () => "OpNavigate" };
    }

    public class JsOperationExecutor : JsOperationBase
    {
        public JsOperationExecutor(JsOperationOptions options)
            : base(options)
        {
            Renderer = () => "new OperationExecutor(" + this.options.ToJS() + ")";
        }

        public JsInstruction DefaultExecute()
        {
            return new JsInstruction(() => "{0}.defaultExecute()".Formato(this.ToJS()));
        }
    }

    public class JsOperationConstructorFrom : JsOperationBase
    {
        public JsOperationConstructorFrom(JsOperationOptions options)
            : base(options)
        {
            Renderer = () => "new ConstructorFrom(" + this.options.ToJS() + ")";
        }

        public JsInstruction DefaultConstruct()
        {
            return new JsInstruction(() => "{0}.defaultConstruct()".Formato(this.ToJS()));
        }
    }

    public class JsOperationConstructorFromMany : JsOperationBase
    {
        public JsOperationConstructorFromMany(JsOperationOptions options)
            : base(options)
        {
            Renderer = () => "new ConstructorFromMany(" + this.options.ToJS() + ")";
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
            Renderer = () =>"new DeleteExecutor(" + this.options.ToJS() + ")";
        }

        public JsInstruction DefaultDelete()
        {
            return new JsInstruction(() => "{0}.defaultDelete()".Formato(this.ToJS()));
        }
    }
}
