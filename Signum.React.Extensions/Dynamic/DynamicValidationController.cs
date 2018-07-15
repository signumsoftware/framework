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
        [Route("api/dynamic/validation/routeTypeName"), HttpPost]
        public string RouteTypeName(PropertyRouteEntity pr)
        {
            return pr.ToPropertyRoute().Type.Name;
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


            var pr = request.dynamicValidation.SubEntity?.ToPropertyRoute() ?? PropertyRoute.Root(TypeLogic.EntityToType.GetOrThrow(request.dynamicValidation.EntityType));
            var candidates = GraphExplorer.FromRootEntity(request.exampleEntity)
                .Where(a => a is ModifiableEntity me && pr.MatchesEntity(me) == true)
                .Cast<ModifiableEntity>();

            var properties = Validator.GetPropertyValidators(pr.Type).Values.Select(a => a.PropertyInfo).ToList();

            try
            {
                return new DynamicValidationTestResponse
                {
                    validationResult = candidates
                    .SelectMany(me => properties.Select(pi => new DynamicValidationResult
                    {
                        propertyName = pi.NiceName(),
                        validationResult = evaluator.EvaluateUntyped(me, pi),
                    }))
                    .ToArray()
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
            public DynamicValidationResult[] validationResult;
        }

        public class DynamicValidationResult {
            public string propertyName;
            public string validationResult;
        }
    }
}
