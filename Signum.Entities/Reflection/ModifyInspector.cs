using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using Signum.Utilities;
using System.Linq;
using Signum.Utilities.Reflection;

namespace Signum.Entities.Reflection
{
    public class ModifyInspector
    {
        static readonly Dictionary<Type, Func<ModifiableEntity, object?>[]> getterCache = new Dictionary<Type, Func<ModifiableEntity, object?>[]>();
        static Func<ModifiableEntity, object?>[] ModifiableFieldGetters(Type type)
        {
            lock (getterCache)
                return getterCache.GetOrCreate(type, () =>
                {
                    FieldInfo[] aux = Reflector.InstanceFieldsInOrder(type);
                    return aux.Where(fi => Reflector.IsModifiableIdentifiableOrLite(fi.FieldType) && !IsIgnored(fi))
                        .Select(fi => ReflectionTools.CreateGetter<ModifiableEntity, object?>(fi)!)
                        .ToArray();
                });
        }

        static readonly Dictionary<Type, Func<ModifiableEntity, object?>[]> getterVirtualCache = new Dictionary<Type, Func<ModifiableEntity, object?>[]>();
        static Func<ModifiableEntity, object?>[] ModifiableFieldGettersVirtual(Type type)
        {
            lock (getterVirtualCache)
                return getterVirtualCache.GetOrCreate(type, () =>
                {
                    FieldInfo[] aux = Reflector.InstanceFieldsInOrder(type);
                    return aux.Where(fi => Reflector.IsModifiableIdentifiableOrLite(fi.FieldType) && (!IsIgnored(fi) || IsQueryableProperty(fi)))
                        .Select(fi => ReflectionTools.CreateGetter<ModifiableEntity, object?>(fi)!)
                        .ToArray();
                });
        }

        private static bool IsIgnored(FieldInfo fi)
        {
            return fi.HasAttribute<IgnoreAttribute>() ||
                (Reflector.FindPropertyInfo(fi).HasAttribute<IgnoreAttribute>());
        }

        private static bool IsQueryableProperty(FieldInfo fi)
        {
            return (Reflector.TryFindPropertyInfo(fi)?.HasAttribute<QueryablePropertyAttribute>() ?? false);
        }


        public static IEnumerable<Modifiable> FullExplore(Modifiable obj)
        {
            if (obj == null)
                yield break;

            if (obj is Lite<IEntity> lite)
            {
                if (lite.EntityOrNull != null)
                    yield return (Entity)lite.EntityOrNull;
            }
            else if (obj is ModifiableEntity mod)
            {
                foreach (var getter in ModifiableFieldGetters(obj.GetType()))
                {
                    object? field = getter(mod);

                    if (field == null)
                        continue;

                    yield return (Modifiable)field;
                }

                foreach (var mixin in mod.Mixins)
                {
                    yield return mixin;
                }
            }
            else if (Reflector.IsMList(obj.GetType()))
            {
                Type t = obj.GetType().ElementType()!;
                if (Reflector.IsModifiableIdentifiableOrLite(t))
                {
                    foreach (var item in (IEnumerable)obj)
                        if (item != null)
                            yield return (Modifiable)item;
                }
            }
            else
                throw new UnexpectedValueException(obj);
        }

        public static IEnumerable<Modifiable> FullExploreVirtual(Modifiable obj)
        {
            if (obj == null)
                yield break;

            if (obj is Lite<IEntity> lite)
            {
                if (lite.EntityOrNull != null)
                    yield return (Entity)lite.EntityOrNull;
            }
            else if (obj is ModifiableEntity mod)
            {
                foreach (var getter in ModifiableFieldGettersVirtual(obj.GetType()))
                {
                    object? field = getter(mod);

                    if (field == null)
                        continue;

                    yield return (Modifiable)field;
                }

                foreach (var mixin in mod.Mixins)
                {
                    yield return mixin;
                }
            }
            else if (Reflector.IsMList(obj.GetType()))
            {
                Type t = obj.GetType().ElementType()!;
                if (Reflector.IsModifiableIdentifiableOrLite(t))
                {
                    foreach (var item in (IEnumerable)obj!)
                        if (item != null)
                            yield return (Modifiable)item;
                }
            }
            else
                throw new UnexpectedValueException(obj);
        }

        public static IEnumerable<Modifiable> EntityExplore(Modifiable obj)
        {
            if (obj == null)//|| obj is Lite)
                yield break;

            if (obj is Lite<IEntity> lite)
            {
                yield break;
            }
            else if (obj is ModifiableEntity mod)
            {
                foreach (var getter in ModifiableFieldGetters(mod.GetType()))
                {
                    object? field = getter(mod);

                    if (field == null || field is Entity)
                        continue;

                    yield return (Modifiable)field;
                }

                foreach (var mixin in mod.Mixins)
                {
                    yield return mixin;
                }

            }
            else if (Reflector.IsMList(obj.GetType()))
            {
                Type t = obj.GetType().ElementType()!;

                if (t.IsModifiable() && !t.IsEntity())
                {
                    foreach (var item in (IEnumerable)obj)
                        if (item != null)
                            yield return (Modifiable)item;
                }
            }
            else
                throw new UnexpectedValueException(obj);
        }
    }
}
