using Signum.Engine.Maps;
using Signum.Utilities.Reflection;

namespace Signum.Basics;

public static class DisableLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Schema.SchemaCompleted += Schema_SchemaCompleted;
    }

    private static void Schema_SchemaCompleted()
    {
        foreach (var kvp in MixinDeclarations.Declarations.Where(kvp => kvp.Value.Contains(typeof(DisabledMixin))))
        {
            giRegisterOperation.GetInvoker(kvp.Key)();
        }
    }

    static GenericInvoker<Action> giRegisterOperation
        = new(() => RegisterOperations<Entity>());
    static void RegisterOperations<T>() where T : Entity
    {
        if (OperationLogic.TryFindOperation(typeof(T), DisableOperation.Disable.Symbol) == null)
            new Graph<T>.Execute(DisableOperation.Disable)
            {
                CanExecute = e => e.Mixin<DisabledMixin>().IsDisabled ? ValidationMessage._0IsSet.NiceToString(ReflectionTools.GetPropertyInfo((DisabledMixin m) => m.IsDisabled).NiceName()) : null,
                Execute = (e, _) => { e.Mixin<DisabledMixin>().IsDisabled = true; },
            }.Register();

        if (OperationLogic.TryFindOperation(typeof(T), DisableOperation.Enabled.Symbol) == null)
            new Graph<T>.Execute(DisableOperation.Enabled)
            {
                CanExecute = e => !e.Mixin<DisabledMixin>().IsDisabled ? ValidationMessage._0IsNotSet.NiceToString(ReflectionTools.GetPropertyInfo((DisabledMixin m) => m.IsDisabled).NiceName()) : null,
                Execute = (e, _) => { e.Mixin<DisabledMixin>().IsDisabled = false; },
            }.Register();
    } 
}
