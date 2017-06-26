using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities
{
    [Serializable, DescriptionOptions(DescriptionOptions.Members), InTypeScript(false)]
    public abstract class MixinEntity : ModifiableEntity
    {
        protected MixinEntity(Entity mainEntity, MixinEntity next)
        {
            this.mainEntity = mainEntity;
            this.next = next;
        }

        [Ignore]
        readonly MixinEntity next;
        [HiddenProperty]
        public MixinEntity Next
        {
            get { return next; }
        }

        [Ignore]
        readonly Entity mainEntity;
        [HiddenProperty]
        public Entity MainEntity
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

        public static ConcurrentDictionary<Type, Func<Entity, MixinEntity, MixinEntity>> Constructors = new ConcurrentDictionary<Type, Func<Entity, MixinEntity, MixinEntity>>();

        public static Action<Type> AssertNotIncluded = t => { throw new NotImplementedException("Call MixinDeclarations.Register in the server, after the Connector is created."); };

        public static void Register<T, M>()
            where T : Entity
            where M : MixinEntity
        {
            Register(typeof(T), typeof(M));
        }

        public static void Register(Type mainEntity, Type mixinEntity)
        {
            if (!typeof(Entity).IsAssignableFrom(mainEntity))
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
                    throw new InvalidOperationException("{0} should have just one non-public construtor with parameters (Entity mainEntity, MixinEntity next)"
                        .FormatWith(me.Name));

                var ci = constructors.Single();

                var pi = ci.GetParameters();

                if (ci.IsPublic || pi.Length != 2 || pi[0].ParameterType != typeof(Entity) || pi[1].ParameterType != typeof(MixinEntity))
                    throw new InvalidOperationException("{0} does not have a non-public construtor with parameters (Entity mainEntity, MixinEntity next)");

                return (Func<Entity, MixinEntity, MixinEntity>)Expression.Lambda(Expression.New(ci, pMainEntity, pNext), pMainEntity, pNext).Compile();
            });
        }

        static readonly ParameterExpression pMainEntity = ParameterExpression.Parameter(typeof(Entity));
        static readonly ParameterExpression pNext = ParameterExpression.Parameter(typeof(MixinEntity));

        public static HashSet<Type> GetMixinDeclarations(Type mainEntity)
        {
            return Declarations.GetOrAdd(mainEntity, me =>
                {
                    var hs = me.GetCustomAttributes(typeof(MixinAttribute), inherit: false)
                        .Cast<MixinAttribute>()
                        .Select(t => t.MixinType)
                        .ToHashSet();

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

        internal static MixinEntity CreateMixins(Entity mainEntity)
        {
            var types = GetMixinDeclarations(mainEntity.GetType());

            MixinEntity result = null;
            foreach (var t in types)
                result = Constructors[t](mainEntity, result);

            return result;
        }

        public static T SetMixin<T, M, V>(this T entity, Expression<Func<M, V>> mixinProperty, V value)
            where T : IEntity
            where M : MixinEntity
        {
            M mixin = ((Entity)(IEntity)entity).Mixin<M>();

            var pi = ReflectionTools.BasePropertyInfo(mixinProperty);

            var setter = MixinSetterCache<M>.Setter<V>(pi);

            setter(mixin, value);

            return entity;
        }

        static class MixinSetterCache<T> where T : MixinEntity
        {
            static ConcurrentDictionary<string, Delegate> cache = new ConcurrentDictionary<string, Delegate>();

            internal static Action<T, V> Setter<V>(PropertyInfo pi)
            {
                return (Action<T, V>)cache.GetOrAdd(pi.Name, s => ReflectionTools.CreateSetter<T, V>(Reflector.FindFieldInfo(typeof(T), pi)));
            }
        }

        public static T CopyMixinsFrom<T>(this T newEntity, IEntity original, params object[] args)
            where T: IEntity
        {
            var list = (from nm in ((Entity)(IEntity)newEntity).Mixins
                        join om in ((Entity)(IEntity)original).Mixins
                        on nm.GetType() equals om.GetType()
                        select new { nm, om });

            foreach (var pair in list)
            {
                pair.nm.CopyFrom(pair.om, args);
            }

            return newEntity;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
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
}
