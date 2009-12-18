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
    public abstract class JsOperationBase
    {
        protected JsOperationOptions options;

        public abstract string ToJS();

        public static JsRenderer Execute(JsOperationExecutor executor)
        {
            return new JsRenderer(() => "OperationExecute({0})".Formato(executor.ToJS()));
        }

        public static JsRenderer ExecutePost(JsOperationExecutor executor)
        {
            return new JsRenderer(() => "OperationExecutePost({0})".Formato(executor.ToJS()));
        }

        public static JsRenderer ConstructFrom(JsOperationConstructorFrom executor)
        {
            return new JsRenderer(() => "OperationConstructFrom({0})".Formato(executor.ToJS()));
        }

        public static JsRenderer ConstructFromPost(JsOperationConstructorFrom executor)
        {
            return new JsRenderer(() => "OperationConstructFromPost({0})".Formato(executor.ToJS()));
        }

        public static JsRenderer Delete(JsOperationDelete executor)
        {
            return new JsRenderer(() => "OperationDelete({0})".Formato(executor.ToJS()));
        }
    }

    public class JsOperationExecutor : JsOperationBase
    {
        public JsOperationExecutor(JsOperationOptions jsOptions)
        {
            this.options = jsOptions;
        }

        public override string ToJS()
        {
            return "new OperationExecutor(" + this.options.ToJS() + ")";
        }
    }

    public class JsOperationConstructorFrom : JsOperationBase
    {
        public JsOperationConstructorFrom(JsOperationOptions jsOptions)
        {
            this.options = jsOptions;
        }

        public override string ToJS()
        {
            return "new ConstructorFrom(" + this.options.ToJS() + ")";
        }
    }

    public class JsOperationDelete : JsOperationBase
    {
        public JsOperationDelete(JsOperationOptions jsOptions)
        {
            this.options = jsOptions;
        }

        public override string ToJS()
        {
            return "new DeleteExecutor(" + this.options.ToJS() + ")";
        }
    }
}
