using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Dynamic;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Entities.Reflection;
using Signum.React.Json;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;

namespace Signum.React.Dynamic
{
    public class DynamicExpressionController : ApiController
    {
        [Route("api/dynamic/expression/test"), HttpPost]
        public DynamicExpressionTestResponse Test(DynamicExpressionTestRequest request)
        {
            IDynamicExpressionEvaluator evaluator;
            var de = request.dynamicExpression;
            try
            {
                var code = $@"
{DynamicCode.GetUsingNamespaces()}

namespace Signum.Entities.Dynamic 
{{
    public class ExprEvaluator : Signum.Entities.Dynamic.IDynamicExpressionEvaluator
    {{
        static Expression<Func<{de.FromType}, {de.ReturnType}>> {de.Name}Expression =
            e => {de.Body};
        //[ExpressionField]
        public static {de.ReturnType} {de.Name}({de.FromType} e)
        {{
            return {de.Name}Expression.Compile().Invoke(e);
        }}

        public object EvaluateUntyped(Entity e){{
            return ExprEvaluator.{de.Name}(({de.FromType})e);
        }}
    }}                   
}}";

                var res = EvalEmbedded<IDynamicExpressionEvaluator>.Compile(DynamicCode.GetAssemblies(), code);

                if (res.CompilationErrors.HasText())
                    throw new InvalidOperationException(res.CompilationErrors);

                evaluator = res.Algorithm;
            }
            catch (Exception e)
            {
                return new DynamicExpressionTestResponse
                {
                    compileError = e.Message
                };
            }

            try
            {
                var result = evaluator.EvaluateUntyped(request.exampleEntity);

                return new DynamicExpressionTestResponse
                {
                    validationResult = Dump(result, request.dynamicExpression.Format) + (request.dynamicExpression.Unit != null ? (" " + request.dynamicExpression.Unit) : "")
                };
            }
            catch (Exception e)
            {
                return new DynamicExpressionTestResponse
                {
                    validationException = e.Message
                };
            }
        }

        private string Dump(object result, string format)
        {
            if (result == null)
                return "null";

            if (result is IFormattable f)
                return f.ToString(format, CultureInfo.CurrentCulture);

            return result.ToString();
        }

        public class DynamicExpressionTestRequest
        {
            public DynamicExpressionEntity dynamicExpression;
            public Entity exampleEntity;
        }

        public class DynamicExpressionTestResponse
        {
            public string compileError;
            public string validationException;
            public string validationResult;
        }
    }
}
