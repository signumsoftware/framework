using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Dynamic;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Signum.React.Filters;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Signum.React.Dynamic
{
    [ValidateModelFilter]
    public class DynamicExpressionController : ControllerBase
    {
        [HttpPost("api/dynamic/expression/test")]
        public DynamicExpressionTestResponse Test([Required, FromBody]DynamicExpressionTestRequest request)
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

                var res = EvalEmbedded<IDynamicExpressionEvaluator>.Compile(DynamicCode.GetCoreMetadataReferences()
                    .Concat(DynamicCode.GetMetadataReferences()), code);

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

        private string Dump(object? result, string? format)
        {
            if (result == null)
                return "null";

            if (result is IFormattable f)
                return f.ToString(format, CultureInfo.CurrentCulture);

            return result.ToString()!;
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
