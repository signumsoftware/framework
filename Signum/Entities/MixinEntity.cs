using System.Collections.Concurrent;
using Signum.Utilities.Reflection;

namespace Signum.Entities;

[DescriptionOptions(DescriptionOptions.Members), InTypeScript(false)]
public abstract class MixinEntity : ModifiableEntity
{
    protected MixinEntity(ModifiableEntity mainEntity, MixinEntity? next)
    {
        this.mainEntity = mainEntity;
        this.next = next;
    }

    [Ignore]
    readonly MixinEntity? next;
    [HiddenProperty]
    public MixinEntity? Next
    {
        get { return next; }
    }

    [Ignore]
    readonly ModifiableEntity mainEntity;
    [HiddenProperty]
    public ModifiableEntity MainEntity
    {
        get { return mainEntity; }
    }

    protected internal virtual void CopyFrom(MixinEntity mixin, object[] args)
    {

    }
}

public static class MixinDeclarations
{
    public static readonly MethodInfo miMixin = ReflectionTools.GetMethodInfo((Entity i) => i.Mixin<CorruptMixin>()).GetGenericMethodDefinition();

    public static ConcurrentDictionary<Type, HashSet<Type>> Declarations = new ConcurrentDictionary<Type, HashSet<Type>>();

    public static ConcurrentDictionary<Type, Func<ModifiableEntity, MixinEntity?, MixinEntity>> Constructors = new ConcurrentDictionary<Type, Func<ModifiableEntity, MixinEntity?, MixinEntity>>();

    public static Action<Type> AssertNotIncluded = t => { throw new NotImplementedException("Call MixinDeclarations.Register in the server, after the Connector is created."); };

    public static void Register<T, M>()
        where T : ModifiableEntity
        where M : MixinEntity
    {
        Register(typeof(T), typeof(M));
    }

    public static void Register(Type mainEntity, Type mixinEntity)
    {
        if (!typeof(ModifiableEntity).IsAssignableFrom(mainEntity))
            throw new InvalidOperationException("{0} is not a {1}".FormatWith(mainEntity.Name, typeof(Entity).Name));

        if (mainEntity.IsAbstract)
            throw new InvalidOperationException("{0} is abstract".FormatWith(mainEntity.Name));

        if (!typeof(MixinEntity).IsAssignableFrom(mixinEntity))
            throw new InvalidOperationException("{0} is not a {1}".FormatWith(mixinEntity.Name, typeof(MixinEntity).Name));

        AssertNotIncluded(mainEntity);

        GetMixinDeclarations(mainEntity).Add(mixinEntity);

        AddConstructor(mixinEntity);
    }

    public static void Import(Dictionary<Type, HashSet<Type>> declarations)
    {
        Declarations = new ConcurrentDictionary<Type, HashSet<Type>>(declarations);

        foreach (var t in declarations.SelectMany(d => d.Value).Distinct())
            AddConstructor(t);
    }

    static void AddConstructor(Type mixinEntity)
    {
        Constructors.GetOrAdd(mixinEntity, me =>
        {
            var constructors = me.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            if (constructors.Length != 1)
                throw new InvalidOperationException($"{me.Name} should have just one non-public construtor with parameters (ModifiableEntity mainEntity, MixinEntity next)"
                    .FormatWith(me.Name));

            var ci = constructors.Single();

            var pi = ci.GetParameters();

            if (ci.IsPublic || pi.Length != 2 || pi[0].ParameterType != typeof(ModifiableEntity) || pi[1].ParameterType != typeof(MixinEntity))
                throw new InvalidOperationException($"{me.Name} does not have a non-public construtor with parameters (ModifiableEntity mainEntity, MixinEntity next)." +
                    (pi[0].ParameterType == typeof(Entity) ? "\nBREAKING CHANGE: The first parameter has changed from Entity -> ModifiableEntity" : null)
                    );

            return (Func<ModifiableEntity, MixinEntity?, MixinEntity>)Expression.Lambda(Expression.New(ci, pMainEntity, pNext), pMainEntity, pNext).Compile();
        });
    }

    static readonly ParameterExpression pMainEntity = ParameterExpression.Parameter(typeof(ModifiableEntity));
    static readonly ParameterExpression pNext = ParameterExpression.Parameter(typeof(MixinEntity));

    public static HashSet<Type> GetMixinDeclarations(Type mainEntity)
    {
        if (!typeof(ModifiableEntity).IsAssignableFrom(mainEntity))
            throw new InvalidOperationException($"Type {mainEntity.Name} is not a ModifiableEntity");

        if (mainEntity.IsAbstract)
            throw new InvalidOperationException($"{mainEntity.Name} is abstract");

        return Declarations.GetOrAdd(mainEntity, me =>
        {
            var hs = new HashSet<Type>(me.GetCustomAttributes(typeof(MixinAttribute), inherit: false)
                .Cast<MixinAttribute>()
                .Select(t => t.MixinType));

            if (EnumEntity.Extract(me) is Type et)
            {
                hs.AddRange(et.GetCustomAttributes(typeof(MixinAttribute), inherit: false).Cast<MixinAttribute>().Select(t => t.MixinType));
            }

            foreach (var t in hs)
                AddConstructor(t);

            return hs;
        });
    }

    public static bool IsDeclared(Type mainEntity, Type mixinType)
    {
        return GetMixinDeclarations(mainEntity).Contains(mixinType);
    }

    public static void AssertDeclared(Type mainEntity, Type mixinType)
    {
        if (!IsDeclared(mainEntity, mixinType))
            throw new InvalidOperationException("Mixin {0} is not registered for {1}. Consider writing MixinDeclarations.Register<{1}, {0}>() at the beginning of Starter.Start".FormatWith(mixinType.TypeName(), mainEntity.TypeName()));
    }

    internal static MixinEntity? CreateMixins(ModifiableEntity mainEntity)
    {
        var types = GetMixinDeclarations(mainEntity.GetType());

        MixinEntity? result = null;
        foreach (var t in types)
            result = Constructors[t](mainEntity, result);

        return result;
    }

    public static T SetMixin<T, M, V>(this T entity, Expression<Func<M, V>> mixinProperty, V value)
        where T : IModifiableEntity
        where M : MixinEntity
    {
        M mixin = ((ModifiableEntity)(IModifiableEntity)entity).Mixin<M>();

        var pi = ReflectionTools.BasePropertyInfo(mixinProperty);

        var setter = MixinSetterCache<M>.Setter<V>(pi);

        setter(mixin, value);

        return entity;
    }

    public static T InitiMixin<T, M>(this T entity, Action<M> initiMixinLambda)
        where T : IModifiableEntity
        where M : MixinEntity
    {
        M mixin = ((ModifiableEntity)(IModifiableEntity)entity).Mixin<M>();
        initiMixinLambda(mixin);
        return entity;
    }

    static class MixinSetterCache<T> where T : MixinEntity
    {
        static ConcurrentDictionary<string, Delegate> cache = new ConcurrentDictionary<string, Delegate>();

        internal static Action<T, V> Setter<V>(PropertyInfo pi)
        {
            return (Action<T, V>)cache.GetOrAdd(pi.Name, s => ReflectionTools.CreateSetter<T, V>(Reflector.FindFieldInfo(typeof(T), pi))!);
        }
    }

    public static T CopyMixinsFrom<T>(this T newEntity, IModifiableEntity original, params object[] args)
        where T: IModifiableEntity
    {
        var list = (from nm in ((ModifiableEntity)(IModifiableEntity)newEntity).Mixins
                    join om in ((ModifiableEntity)(IModifiableEntity)original).Mixins
                    on nm.GetType() equals om.GetType()
                    select new { nm, om });

        foreach (var pair in list)
        {
            pair.nm!.CopyFrom(pair.om, args);
        }

        return newEntity;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, Inherited = false, AllowMultiple = true)]
public sealed class MixinAttribute : Attribute
{
    readonly Type mixinType;
    public MixinAttribute(Type mixinType)
    {
        this.mixinType = mixinType;
    }

    public Type MixinType
    {
        get { return this.mixinType; }
    }
}
