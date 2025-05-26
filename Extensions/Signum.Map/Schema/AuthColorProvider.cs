using Signum.Authorization.Rules;
using Signum.Authorization;

namespace Signum.Map;

public class AuthColorProvider
{
    public static MapColorProvider[] GetMapColors()
    {
        if (!BasicPermission.AdminRules.IsAuthorized())
            return new MapColorProvider[0];

        var roleRules = AuthLogic.RolesInOrder(includeTrivialMerge: false).ToDictionary(r => r,
            r => TypeAuthLogic.GetTypeRulesSimple(r).ToDictionary(a => TypeLogic.GetCleanName(a.Key), a => a.Value));

        return roleRules.Keys.Select((r, i) => new MapColorProvider
        {
            Name = "role-" + r.Key(),
            NiceName = "Role - " + r.ToString(),
            AddExtra = t =>
            {
                var tac = roleRules[r].TryGetC(t.typeName);

                if (tac == null)
                    return;

                t.extra["role-" + r.Key() + "-ui"] = GetName(ToStringList(tac, userInterface: true));
                t.extra["role-" + r.Key() + "-db"] = GetName(ToStringList(tac, userInterface: false));
                t.extra["role-" + r.Key() + "-tooltip"] = ToString(tac.Fallback) + "\n" + (tac.ConditionRules.IsNullOrEmpty() ? null :
                    tac.ConditionRules.ToString(a => a.ToString() + ": " + ToString(a.Allowed), "\n") + "\n");
            },
            Order = 10,
        }).ToArray();
    }

    static string GetName(List<TypeAllowedBasic> list)
    {
        return "auth-" + list.ToString("-");
    }

    static List<TypeAllowedBasic> ToStringList(WithConditions<TypeAllowed> tac, bool userInterface)
    {
        List<TypeAllowedBasic> result = [tac.Fallback.Get(userInterface)];

        foreach (var c in tac.ConditionRules)
            result.Add(c.Allowed.Get(userInterface));

        return result;
    }

    private static string ToString(TypeAllowed? typeAllowed)
    {
        if (typeAllowed == null)
            return "MERGE ERROR!";

        if (typeAllowed.Value.GetDB() == typeAllowed.Value.GetUI())
            return typeAllowed.Value.GetDB().NiceToString();

        return "DB {0} / UI {1}".FormatWith(typeAllowed.Value.GetDB().NiceToString(), typeAllowed.Value.GetUI().NiceToString());
    }
}
