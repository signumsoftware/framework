using System.Collections.Frozen;
using System.Data;
using System.Net.Http.Headers;
using System.Xml.Linq;

namespace Signum.Authorization.Rules;


public abstract class AuthCache<RT, AR, R, K, A, AM> : IManualAuth<K, A>
    where RT : RuleEntity<R>, new()
    where AR : AllowedRule<R, AM>, new()
    where A : notnull
    where AM : notnull
    where R : class
    where K : notnull
{
    readonly ResetLazy<FrozenDictionary<Lite<RoleEntity>, RoleAllowedCache>> runtimeRules;

   
    public AuthCache(SchemaBuilder sb, bool invalidateWithTypes)
    {
       
        runtimeRules = sb.GlobalLazy(this.NewCache,
            invalidateWithTypes ?
            new InvalidateWith(typeof(RT), typeof(RoleEntity), typeof(RuleTypeEntity)) :
            new InvalidateWith(typeof(RT), typeof(RoleEntity)),
            AuthLogic.NotifyRulesChanged);
    }

    protected abstract Expression<Func<R, R, bool>> IsEqual { get; }

    protected abstract K ToKey(R resource);
    protected abstract R ToEntity(K key);
    protected abstract A Merge(K key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, A>> baseValues);
    protected abstract Func<K, A> MergeDefault(Lite<RoleEntity> role);

    protected abstract A GetRuleAllowed(RT rule);
    protected abstract RT SetRuleAllowed(RT rule, A allowed);
    protected abstract AM ToAllowedModel(A allowed);
    protected abstract A ToAllowed(AM allowedModel);

    public virtual A CoerceValue(Lite<RoleEntity> role, K key, A allowed, bool manual = false)
    {
        return allowed;
    }

    protected virtual AR ToAllowedRule(R resource, RoleAllowedCache ruleCache)
    {
        var k = ToKey(resource);

        return new AR()
        {
            Resource = resource,
            AllowedBase = ToAllowedModel(ruleCache.GetAllowedBase(k)),
            Allowed = ToAllowedModel(ruleCache.GetAllowed(k))
        };
    }

    

    A IManualAuth<K, A>.GetAllowed(Lite<RoleEntity> role, K key)
    {
        R resource = ToEntity(key);

        ManualResourceCache miniCache = new ManualResourceCache(this, key, resource);

        return miniCache.GetAllowed(role);
    }

    void IManualAuth<K, A>.SetAllowed(Lite<RoleEntity> role, K key, A allowed)
    {
        R resource = ToEntity(key);

        ManualResourceCache miniCache = new ManualResourceCache(this, key, resource);

        allowed = CoerceValue(role, key, allowed, manual: true);

        if (miniCache.GetAllowed(role).Equals(allowed))
            return;

        IQueryable<RT> query = Database.Query<RT>().Where(a =>this.IsEqual.Evaluate(a.Resource, resource) && a.Role.Is(role));
        if (miniCache.GetAllowedBase(role).Equals(allowed))
        {
            if (query.UnsafeDelete() == 0)
                throw new InvalidOperationException("Inconsistency in the data");
        }
        else
        {
            query.UnsafeDelete();
            SetRuleAllowed(new RT
            {
                Role = role,
                Resource = resource,
            }, allowed).Save();
        }
    }

    public class ManualResourceCache
    {
        readonly AuthCache<RT, AR, R, K, A, AM> cache; 

        readonly Dictionary<Lite<RoleEntity>, A> specificRules;

        readonly K key;

        public ManualResourceCache(AuthCache<RT, AR, R, K, A, AM> cache, K key, R resource)
        {
            this.key = key;
            this.cache = cache;
            var list = Database.Query<RT>().Where(r => cache.IsEqual.Evaluate(r.Resource, resource)).ToList();

            specificRules = list.ToDictionary(a => a.Role, a => cache.GetRuleAllowed(a));
        }

        public A GetAllowed(Lite<RoleEntity> role)
        {
            if (specificRules.TryGetValue(role, out A? result))
                return cache.CoerceValue(role, key, result, manual: true);

            return GetAllowedBase(role);
        }

        public A GetAllowedBase(Lite<RoleEntity> role)
        {
            var result = cache.Merge(key, role, AuthLogic.RelatedTo(role).Select(r => KeyValuePair.Create(r, GetAllowed(r))));

            return cache.CoerceValue(role, key, result, manual: true);
        }
    }

    internal bool HasRealOverrides(Lite<RoleEntity> role)
    {
        return Database.Query<RT>().Any(rt => rt.Role.Is(role));
    }

    FrozenDictionary<Lite<RoleEntity>, RoleAllowedCache> NewCache()
    {
        List<Lite<RoleEntity>> roles = AuthLogic.RolesInOrder(includeTrivialMerge: true).ToList();

        var rules = Database.Query<RT>().ToList();

        var errors = GraphExplorer.FullIntegrityCheck(GraphExplorer.FromRoots(rules));
        if (errors != null)
            throw new IntegrityCheckException(errors);

        Dictionary<Lite<RoleEntity>, Dictionary<K, A>> realRules = rules
              .AgGroupToDictionary(ru => ru.Role!, gr => gr
                .SelectCatch(ru => KeyValuePair.Create(ToKey(ru.Resource!), GetRuleAllowed(ru)))
                .ToDictionaryEx());

        Dictionary<Lite<RoleEntity>, RoleAllowedCache> newRules = new Dictionary<Lite<RoleEntity>, RoleAllowedCache>();
        foreach (var role in roles)
        {
            var related = AuthLogic.RelatedTo(role);

            newRules.Add(role, new RoleAllowedCache(this, role,
                related.Select(r => newRules.GetOrThrow(r)).ToList(),
                realRules.TryGetC(role)));
        }

        return newRules.ToFrozenDictionary();
    }



    internal void GetRules(BaseRulePack<AR> pack, IEnumerable<R> resources)
    {
        RoleAllowedCache ruleCache = runtimeRules.Value.GetOrThrow(pack.Role);

        pack.MergeStrategy = AuthLogic.GetMergeStrategy(pack.Role);
        pack.InheritFrom = AuthLogic.RelatedTo(pack.Role).ToMList();
        pack.Rules = resources.Select(r => ToAllowedRule(r, ruleCache)).ToMList();
    }

    internal void SetRules(BaseRulePack<AR> rules, Expression<Func<R, bool>> filterResources)
    {
        using (AuthLogic.Disable())
        {
            var current = Database.Query<RT>().Where(r => r.Role.Is(rules.Role) && filterResources.Evaluate(r.Resource)).ToDictionary(a => a.Resource);
            var should = rules.Rules.Where(a => a.Overriden).ToDictionary(r => r.Resource);

            Synchronizer.Synchronize(should, current,
                (p, ar) => {
                    var rule = SetRuleAllowed(new RT { Resource = p, Role = rules.Role },  ToAllowed(ar.Allowed));
                    rule.Save();

                    },
                (p, rule) => rule.Delete(),
                (p, ar, rule) =>
                {
                    SetRuleAllowed(rule, ToAllowed(ar.Allowed));
                    if (rule.IsGraphModified)
                        rule.Save();
                });
        }
    }

    internal A GetAllowed(Lite<RoleEntity> role, K key)
    {
        return runtimeRules.Value.GetOrThrow(role).GetAllowed(key);
    }

    internal A GetAllowedBase(Lite<RoleEntity> role, K key)
    {
        return runtimeRules.Value.GetOrThrow(role).GetAllowedBase(key);
    }

    internal DefaultDictionary<K, A> GetDefaultDictionary()
    {
        return runtimeRules.Value.GetOrThrow(RoleEntity.Current).DefaultDictionary();
    }

    internal DefaultDictionary<K, A> GetDefaultDictionary(Lite<RoleEntity> role)
    {
        return runtimeRules.Value.GetOrThrow(role).DefaultDictionary();
    }

    public class RoleAllowedCache
    {
        readonly AuthCache<RT, AR, R, K, A, AM> cache;
        internal readonly Lite<RoleEntity> Role;

        readonly DefaultDictionary<K, A> rules;
        readonly List<RoleAllowedCache> baseCaches;


        public RoleAllowedCache(AuthCache<RT, AR, R, K, A, AM>  cache, Lite<RoleEntity> role, List<RoleAllowedCache> baseCaches, Dictionary<K, A>? newValues)
        {
            this.Role = role;
            this.cache = cache;
            this.baseCaches = baseCaches;

            Func<K, A> defaultAllowed = cache.MergeDefault(role);

            Func<K, A> baseAllowed = k => cache.Merge(k, role, baseCaches.Select(b => KeyValuePair.Create(b.Role, b.GetAllowed(k))));

            var keys = baseCaches
                .Where(b => b.rules.OverrideDictionary != null)
                .SelectMany(a => a.rules.OverrideDictionary!.Keys)
                .ToHashSet();

            Dictionary<K, A>? tmpRules = keys.ToDictionary(k => k, baseAllowed);
            if (newValues != null)
                tmpRules.SetRange(newValues);

            tmpRules = Simplify(tmpRules, defaultAllowed, baseAllowed);

            rules = new DefaultDictionary<K, A>(defaultAllowed, tmpRules);
        }

        Dictionary<K, A>? Simplify(Dictionary<K, A> dictionary, Func<K, A> defaultAllowed, Func<K, A> baseAllowed)
        {
            if (dictionary == null || dictionary.Count == 0)
                return null;

            dictionary.RemoveRange(dictionary.Where(p =>
                p.Value.Equals(defaultAllowed(p.Key)) &&
                p.Value.Equals(baseAllowed(p.Key))).Select(p => p.Key).ToList());

            if (dictionary.Count == 0)
                return null;

            return dictionary;
        }

        public A GetAllowed(K key)
        {
            var raw = rules.GetAllowed(key);

            return cache.CoerceValue(Role, key, raw);
        }

        public A GetAllowedBase(K key)
        {
            var raw = this.cache.Merge(key, Role, baseCaches.Select(b => KeyValuePair.Create(b.Role, b.GetAllowed(key))));

            return cache.CoerceValue(Role, key, raw);
        }

        internal DefaultDictionary<K, A> DefaultDictionary()
        {
            return rules;
        }
    }

    public abstract XElement ExportXml(bool exportAll);

    protected XElement ExportXmlInternal(XName rootName, XName elementName, Func<K, string> resourceToString, Func<A, object[]> allowedToXml, List<K>? allKeys)
    {
        var rules = runtimeRules.Value;

        return new XElement(rootName,
            from r in AuthLogic.RolesInOrder(includeTrivialMerge: false)
            let rac = rules.GetOrThrow(r)

            select new XElement("Role",
                new XAttribute("Name", r.ToString()!),
                    from k in allKeys ?? (rac.DefaultDictionary().OverrideDictionary?.Keys).EmptyIfNull()
                    let allowedBase = rac.GetAllowedBase(k)
                    let allowed = rac.GetAllowed(k)
                    where allKeys != null || !allowed.Equals(allowedBase)
                    let resource = resourceToString(k)
                    orderby resource
                    select new XElement(elementName,
                       new XAttribute("Resource", resource),
                       allowedToXml(allowed))
                       )
            );
    }


    public abstract SqlPreCommand? ImportXml(XElement root, Dictionary<string, Lite<RoleEntity>> roles, Replacements replacements);

    protected SqlPreCommand? ImportXmlInternal(XElement root, XName rootName, XName elementName, Dictionary<string, Lite<RoleEntity>> roles,
        Func<string, R?> toResource, Func<XElement, A> parseAllowed)
    {
        var current = Database.RetrieveAll<RT>().GroupToDictionary(a => a.Role);
        var xRoles = (root.Element(rootName)?.Elements("Role")).EmptyIfNull();
        var should = xRoles.ToDictionary(x => roles.GetOrThrow(x.Attribute("Name")!.Value));

        Table table = Schema.Current.Table(typeof(RT));

        return Synchronizer.SynchronizeScript(Spacing.Double, should, current,
            createNew: (role, x) =>
            {
                var dic = (from xr in x.Elements(elementName)
                           let r = toResource(xr.Attribute("Resource")!.Value)
                           where r != null
                           select KeyValuePair.Create(r, parseAllowed(xr)))
                           .ToDictionaryEx("{0} rules for {1}".FormatWith(typeof(R).NiceName(), role));

                SqlPreCommand? restSql = dic.Select(kvp => table.InsertSqlSync(SetRuleAllowed(new RT
                {
                    Resource = kvp.Key,
                    Role = role,
                }, kvp.Value), comment: Comment(role, kvp.Key, kvp.Value))).Combine(Spacing.Simple)?.Do(p => p.GoBefore = true);

                return restSql;
            },
            removeOld: (role, list) => list.Select(rt => table.DeleteSqlSync(rt, null)).Combine(Spacing.Simple)?.Do(p => p.GoBefore = true),
            mergeBoth: (role, x, list) =>
            {
                var def = list.SingleOrDefaultEx(a => a.Resource == null);

                var shouldResources = (from xr in x.Elements(elementName)
                                       let r = toResource(xr.Attribute("Resource")!.Value)
                                       where r != null
                                       select KeyValuePair.Create(ToKey(r), xr))
                           .ToDictionaryEx("{0} rules for {1}".FormatWith(typeof(R).NiceName(), role));

                var currentResources = list.Where(a => a.Resource != null).ToDictionary(a => ToKey(a.Resource));

                SqlPreCommand? restSql = Synchronizer.SynchronizeScript(Spacing.Simple, shouldResources, currentResources,
                    (r, xr) =>
                    {
                        var a = parseAllowed(xr);
                        return table.InsertSqlSync(SetRuleAllowed(new RT { Resource = ToEntity(r), Role = role }, a), comment: Comment(role, ToEntity(r), a));
                    },
                    (r, rt) => table.DeleteSqlSync(rt, null, Comment(role, ToEntity(r), GetRuleAllowed(rt))),
                    (r, xr, rt) =>
                    {
                        var oldA = GetRuleAllowed(rt);
                        SetRuleAllowed(rt, parseAllowed(xr));
                        if (rt.IsGraphModified)
                            return table.UpdateSqlSync(rt, null, comment: Comment(role, ToEntity(r), oldA, GetRuleAllowed(rt)));
                        return null;
                    })?.Do(p => p.GoBefore = true);

                return restSql;
            });
    }


    internal string Comment(Lite<RoleEntity> role, R resource, A allowed)
    {
        return "{0} {1} for {2} ({3})".FormatWith(typeof(R).NiceName(), resource.ToString(), role, AllowedComment(allowed));
    }

    protected virtual string AllowedComment(A allowed) 
    {
        return allowed.ToString()!;
    }

    internal string Comment(Lite<RoleEntity> role, R resource, A from, A to)
    {
        return "{0} {1} for {2} ({3} -> {4})".FormatWith(typeof(R).NiceName(), resource.ToString(), role, AllowedComment(from), AllowedComment(to));
    }
}

public interface IManualAuth<K, A>
{
    A GetAllowed(Lite<RoleEntity> role, K key);
    void SetAllowed(Lite<RoleEntity> role, K key, A allowed);
}
