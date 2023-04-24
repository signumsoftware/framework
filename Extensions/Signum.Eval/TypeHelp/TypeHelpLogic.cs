using Signum.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Eval.TypeHelp;


public static class TypeHelpLogic
{
    public static ResetLazy<HashSet<Type>> AvailableEmbeddedEntities = null!;
    public static ResetLazy<HashSet<Type>> AvailableModelEntities = null!;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            AvailableEmbeddedEntities = sb.GlobalLazy(() =>
            {
                var namespaces = EvalLogic.GetNamespaces().ToHashSet();
                return EvalLogic.AssemblyTypes
                .Select(t => t.Assembly)
                .Distinct()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(EmbeddedEntity).IsAssignableFrom(t) && namespaces.Contains(t.Namespace!))
                .ToHashSet();

            }, new InvalidateWith());

            AvailableModelEntities = sb.GlobalLazy(() =>
            {
                var namespaces = EvalLogic.GetNamespaces().ToHashSet();
                return EvalLogic.AssemblyTypes
                .Select(t => t.Assembly)
                .Distinct()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(ModelEntity).IsAssignableFrom(t) && namespaces.Contains(t.Namespace!))
                .ToHashSet();

            }, new InvalidateWith());

            if (sb.WebServerBuilder != null)
            {
                ReflectionServer.RegisterLike(typeof(TypeHelpMessage), () => UserHolder.Current != null);
            }
        }
    }
}
