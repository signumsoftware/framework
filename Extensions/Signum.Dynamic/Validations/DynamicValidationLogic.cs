
using System.Collections.Frozen;

namespace Signum.Dynamic.Validations;

public static class DynamicValidationLogic
{
    class DynamicValidationPair
    {
        public PropertyRoute PropertyRoute;
        public DynamicValidationEntity Validation;

        public DynamicValidationPair(PropertyRoute propertyRoute, DynamicValidationEntity validation)
        {
            PropertyRoute = propertyRoute;
            Validation = validation;
        }
    }

    static ResetLazy<FrozenDictionary<Type, List<DynamicValidationPair>>> DynamicValidations = null!;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<DynamicValidationEntity>()
            .WithSave(DynamicValidationOperation.Save)
            .WithDelete(DynamicValidationOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Name,
                e.EntityType,
                e.SubEntity,
                Script = e.Eval.Script.Etc(75),
            });

        new Graph<DynamicValidationEntity>.ConstructFrom<DynamicValidationEntity>(DynamicValidationOperation.Clone)
        {
            Construct = (dv, _) => new DynamicValidationEntity()
            {
                Name = dv.Name,
                EntityType = dv.EntityType,
                SubEntity = dv.SubEntity,
                Eval = new DynamicValidationEval() { Script = dv.Eval.Script },
            }
        }.Register();


        DynamicValidations = sb.GlobalLazy(() => Database.Query<DynamicValidationEntity>()
                .SelectCatch(dv => new DynamicValidationPair(dv.SubEntity?.ToPropertyRoute() ?? PropertyRoute.Root(TypeLogic.EntityToType.GetOrThrow(dv.EntityType)), dv))
                .GroupToFrozenDictionary(a => a.PropertyRoute.Type),
        new InvalidateWith(typeof(DynamicValidationEntity)));

        DynamicValidationEntity.GetMainType = dve => dve.SubEntity?.ToPropertyRoute().Type ?? TypeLogic.EntityToType.GetOrThrow(dve.EntityType);

        sb.Schema.Initializing += () => { initialized = true; };

        Validator.GlobalValidation += DynamicValidation;
        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<DynamicValidationEntity>().Where(dv => dv.EntityType.Is(type)));
    }
    static bool initialized = false;

    public static string? DynamicValidation(ModifiableEntity mod, PropertyInfo pi)
    {
        if (!initialized)
            return null;

        var candidates = DynamicValidations.Value.TryGetC(mod.GetType()).EmptyIfNull()
            .Where(pair => pair.Validation.Mixin<DisabledMixin>().IsDisabled == false)
            .ToList();

        if (candidates.IsEmpty())
            return null;

        foreach (var pair in candidates)
        {
            if (pair.PropertyRoute.MatchesEntity(mod) == true)
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
                        throw;
                    }
                }
            }
        }

        return null;
    }
}
