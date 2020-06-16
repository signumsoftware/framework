using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.Tree;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Entities.Tree;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Basics
{
    public static class DisableLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Schema.SchemaCompleted += Schema_SchemaCompleted;
            }
        }

        private static void Schema_SchemaCompleted()
        {
            foreach (var kvp in MixinDeclarations.Declarations.Where(kvp => kvp.Value.Contains(typeof(DisabledMixin))))
            {
                if (typeof(TreeEntity).IsAssignableFrom(kvp.Key))
                    giRegisterTreeOperation.GetInvoker(kvp.Key)();
                else
                    giRegisterOperation.GetInvoker(kvp.Key)();
            }
        }

        static GenericInvoker<Action> giRegisterOperation
            = new GenericInvoker<Action>(() => RegisterOperations<Entity>());
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

        static GenericInvoker<Action> giRegisterTreeOperation
          = new GenericInvoker<Action>(() => RegisterTreeOperations<TreeEntity>());
        static void RegisterTreeOperations<T>() where T : TreeEntity
        {
            if (OperationLogic.TryFindOperation(typeof(T), DisableOperation.Disable.Symbol) == null)
                new Graph<T>.Execute(DisableOperation.Disable)
                {
                    CanExecute = e => e.Mixin<DisabledMixin>().IsDisabled ? DisabledIsSetMessage() : null,
                    Execute = (e, _) =>
                    {
                        e.Mixin<DisabledMixin>().IsDisabled = true;
                        e.Save();
                        var children = e.Children().Where(a => a.Mixin<DisabledMixin>().IsDisabled == false).ToList();
                        foreach (var item in children)
                        {
                            item.Execute(DisableOperation.Disable);
                        }

                    },
                }.Register();

            if (OperationLogic.TryFindOperation(typeof(T), DisableOperation.Enabled.Symbol) == null)
                new Graph<T>.Execute(DisableOperation.Enabled)
                {
                    CanExecute = e =>
                    !e.Mixin<DisabledMixin>().IsDisabled ? DisabledIsNotSetMessage() :
                    e.InDBEntity(_ => (bool?)_.Parent()!.Mixin<DisabledMixin>().IsDisabled) == true ? DisabledMessage.ParentIsDisabled.NiceToString() :
                    null,
                    Execute = (e, _) =>
                    {
                        e.Mixin<DisabledMixin>().IsDisabled = false;
                        e.Save();
                        var children = e.Children().Where(a => a.Mixin<DisabledMixin>().IsDisabled == true).ToList();
                        foreach (var item in children)
                        {
                            item.Execute(DisableOperation.Enabled);
                        }
                    },
                }.Register();
        }

        public static string DisabledIsNotSetMessage() => ValidationMessage._0IsNotSet.NiceToString(ReflectionTools.GetPropertyInfo((DisabledMixin m) => m.IsDisabled).NiceName());
        public static string DisabledIsSetMessage() => ValidationMessage._0IsSet.NiceToString(ReflectionTools.GetPropertyInfo((DisabledMixin m) => m.IsDisabled).NiceName());
    }
}
