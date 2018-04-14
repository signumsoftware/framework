using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Dynamic
{
    public static class DynamicValidationLogic
    {
        class DynamicValidationPair
        {
            public PropertyRoute PropertyRoute;
            public DynamicValidationEntity Validation;
        }

        static ResetLazy<Dictionary<PropertyInfo, List<DynamicValidationPair>>> DynamicValidations; 

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DynamicValidationEntity>()
                    .WithSave(DynamicValidationOperation.Save)
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.EntityType,
                        e.PropertyRoute,
                        e.Eval,
                    });
                DynamicValidations = sb.GlobalLazy(() => Database.Query<DynamicValidationEntity>()
                        .SelectCatch(dv => new DynamicValidationPair { Validation = dv, PropertyRoute = dv.PropertyRoute.ToPropertyRoute() })
                        .GroupToDictionary(a => a.PropertyRoute.PropertyInfo),
                new InvalidateWith(typeof(DynamicValidationEntity)));

                DynamicValidationEntity.GetMainType = dve => dve.PropertyRoute?.ToPropertyRoute().Parent.Type;

                sb.Schema.Initializing += () => { initialized = true; };

                Validator.GlobalValidation += DynamicValidation;
                sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<DynamicValidationEntity>().Where(dv => dv.EntityType == type));
            }
        }
        static bool initialized = false;

        public static string DynamicValidation(ModifiableEntity mod, PropertyInfo pi)
        {
            if (!initialized)
                return null;

            var candidates = DynamicValidations.Value.TryGetC(pi);
            if (candidates == null)
                return null;

            foreach (var pair in candidates)
            {
                if (pair.PropertyRoute.MatchesProperty(mod, pi) == true)
                {
                    var val = pair.Validation;

                    using (HeavyProfiler.LogNoStackTrace("DynamicValidation", () => val.Name))
                    {
                        try
                        {
                            string result = val.Eval.Algorithm.EvaluateUntyped(mod, pi);
                            if (result != null)
                                return result;
                        }
                        catch (Exception e)
                        {
                            e.Data["DynamicValidation"] = val.Name;
                            throw e;
                        }
                    }
                }
            }

            return null;
        }
    }
}
