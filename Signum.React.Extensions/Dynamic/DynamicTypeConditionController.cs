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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Signum.React.ApiControllers;

namespace Signum.React.Dynamic
{
    public class DynamicTypeConditionController : ApiController
    {
        [HttpPost("api/dynamic/typeCondition/test")]
        public DynamicTypeConditionTestResponse Test([FromBody]DynamicTypeConditionTestRequest request)
        {
            IDynamicTypeConditionEvaluator evaluator;
            try
            {
                evaluator = request.dynamicTypeCondition.Eval.Algorithm;
            }
            catch(Exception e)
            {
                return new DynamicTypeConditionTestResponse
                {
                    compileError = e.Message
                };
            }

            try
            {
                return new DynamicTypeConditionTestResponse
                {
                    validationResult = evaluator.EvaluateUntyped(request.exampleEntity)
                };
            }
            catch (Exception e)
            {
                return new DynamicTypeConditionTestResponse
                {
                    validationException = e.Message
                };
            }
        }

        public class DynamicTypeConditionTestRequest
        {
            public DynamicTypeConditionEntity dynamicTypeCondition;
            public Entity exampleEntity;
        }

        public class DynamicTypeConditionTestResponse
        {
            public string compileError;
            public string validationException;
            public bool validationResult;
        }
    }
}
