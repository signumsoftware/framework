using Signum.Authorization.Rules;
using Signum.Authorization;
using Signum.Dynamic.Views;
using Signum.Workflow;
using Signum.Utilities.Reflection;
using Signum.Dynamic.Types;

namespace Signum.WorkflowDynamic;

public static class WorkflowDynamicLogic
{
  
    public static void Start(SchemaBuilder sb, params Type[] registerExpressionsFor)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => WorkflowLogic.Start(null!, null!)));

        sb.Schema.WhenIncluded<DynamicTypeEntity>(() =>
        {
            new Graph<DynamicTypeEntity>.Execute(DynamicTypeWorkflowOperation.FixCaseDescriptions)
            {
                Execute = (e, _) =>
                {
                    var type = TypeLogic.GetType(e.TypeName);
                    giFixCaseDescriptions.GetInvoker(type)();
                },
            }.Register();
        });

        sb.Schema.WhenIncluded<DynamicViewEntity>(() =>
        {
            Validator.PropertyValidator((WorkflowActivityEntity a) => a.ViewNameProps).StaticPropertyValidation = (wam, pi) =>
            {
                if (wam.ViewName.HasText())
                {
                    var dv = DynamicViewEntity.TryGetDynamicView(wam.Lane.Pool.Workflow.MainEntityType.ToType(), wam.ViewName);
                    if (dv != null)
                        return ValidateViewNameProps(dv, wam.ViewNameProps);
                }

                return null;
            };

            Validator.PropertyValidator((WorkflowActivityModel a) => a.ViewNameProps).StaticPropertyValidation = (wam, pi) =>
             {
                 if (wam.ViewName.HasText() && wam.Workflow != null)
                 {
                     var dv = DynamicViewEntity.TryGetDynamicView(wam.Workflow.MainEntityType.ToType(), wam.ViewName);
                     if (dv != null)
                         return ValidateViewNameProps(dv, wam.ViewNameProps);
                 }
                
                 return null;
             };
        });
    }

    static readonly GenericInvoker<Action> giFixCaseDescriptions = new(() => FixCaseDescriptions<Entity>());

    public static void FixCaseDescriptions<T>() where T : Entity
    {
        Database.Query<CaseEntity>()
                      .Where(a => a.MainEntity.GetType() == typeof(T))
                      .UnsafeUpdate()
                      .Set(a => a.Description, a => ((T)a.MainEntity).ToString())
                      .Execute();
    }


    internal static string? ValidateViewNameProps(DynamicViewEntity dv, MList<ViewNamePropEmbedded> viewNameProps)
    {
        var extra = viewNameProps.Where(a => !dv.Props.Any(p => p.Name == a.Name)).CommaAnd(a => a.Name);
        var missing = dv.Props.Where(p => !p.Type.EndsWith("?") && !viewNameProps.Any(a => a.Expression.HasText() && p.Name == a.Name)).CommaAnd(a => a.Name);

        return " and ".Combine(
            extra.HasText() ? "The ViewProps " + extra + " are not declared in " + dv.ViewName : null,
            missing.HasText() ? "The ViewProps " + missing + " are mandatory in " + dv.ViewName : null
            ).DefaultToNull();
    }
}
