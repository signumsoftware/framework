using Signum.API;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Eval.TypeHelp;


public static class TypeHelpLogic
{
    public static ResetLazy<FrozenSet<Type>> AvailableEmbeddedEntities = null!;
    public static ResetLazy<FrozenSet<Type>> AvailableModelEntities = null!;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        AvailableEmbeddedEntities = sb.GlobalLazy(() =>
        {
            var namespaces = EvalLogic.GetNamespaces().ToHashSet();
            return EvalLogic.AssemblyTypes
            .Select(t => t.Assembly)
            .Distinct()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(EmbeddedEntity).IsAssignableFrom(t) && namespaces.Contains(t.Namespace!))
            .ToFrozenSet();

        }, new InvalidateWith());

        AvailableModelEntities = sb.GlobalLazy(() =>
        {
            var namespaces = EvalLogic.GetNamespaces().ToHashSet();
            return EvalLogic.AssemblyTypes
            .Select(t => t.Assembly)
            .Distinct()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(ModelEntity).IsAssignableFrom(t) && namespaces.Contains(t.Namespace!))
            .ToFrozenSet();

        }, new InvalidateWith());

        if (sb.WebServerBuilder != null)
        {
            ReflectionServer.RegisterLike(typeof(TypeHelpMessage), () => UserHolder.Current != null);
        }
    }
}
