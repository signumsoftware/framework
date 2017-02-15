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
using System.Web.Http;

namespace Signum.React.Dynamic
{
    public class DynamicValidationController : ApiController
    {
        [Route("api/dynamic/validation/parentType"), HttpPost]
        public string ParentType(PropertyRouteEntity pr)
        {
            return pr.ToPropertyRoute().Parent.Type.Name;
        }

        [Route("api/dynamic/validation/test"), HttpPost]
        public DynamicValidationTestResponse Test(DynamicValidationTestRequest request)
        {
            IDynamicValidationEvaluator evaluator;
            try
            {
                evaluator = request.dynamicValidation.Eval.Algorithm;
            }
            catch(Exception e)
            {
                return new DynamicValidationTestResponse
                {
                    compileError = e.Message
                };
            }


            var pr = request.dynamicValidation.PropertyRoute.ToPropertyRoute();
            var candidates = GraphExplorer.FromRootEntity(request.exampleEntity)
                .Where(a => a.GetType() == pr.Parent.Type && pr.MatchesProperty((ModifiableEntity)a, pr.PropertyInfo) == true)
                .Cast<ModifiableEntity>();

            try
            {
                return new DynamicValidationTestResponse
                {
                    validationResult = candidates.Select(me => evaluator.EvaluateUntyped(me, pr.PropertyInfo)).ToArray()
                };
            }
            catch (Exception e)
            {
                return new DynamicValidationTestResponse
                {
                    validationException = e.Message
                };
            }
        }

        public class DynamicValidationTestRequest
        {
            public DynamicValidationEntity dynamicValidation;
            public Entity exampleEntity;
        }

        public class DynamicValidationTestResponse
        {
            public string compileError;
            public string validationException;
            public string[] validationResult;
        }
    }
}
