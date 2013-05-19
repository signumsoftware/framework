using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;
using Signum.Entities;
using Signum.Utilities;
using System.Linq;
using System.Security.Permissions;
using Signum.Utilities.Reflection;

namespace Signum.Entities.Reflection
{
    public class ModifyInspector
    {
        //dicconario con los field info con posibles modificables de los modificables normales
        static Dictionary<Type, Func<object, object>[]> getterCache = new Dictionary<Type, Func<object, object>[]>();

        static Func<object, object>[] ModifiableFieldGetters(Type type)
        {
            lock (getterCache)
                return getterCache.GetOrCreate(type, () =>
                {
                    FieldInfo[] aux = Reflector.InstanceFieldsInOrder(type);
                    return aux.Where(fi => Reflector.IsModifiableIdentifiableOrLite(fi.FieldType) && !fi.HasAttribute<IgnoreAttribute>())
                        .Select(fi => ReflectionTools.CreateGetterUntyped(type, fi)).ToArray();
                });
        }


        /// <summary>
        /// Devuelve todos los Modificables que haya dentro de obj
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable<Modifiable> FullExplore(Modifiable obj)
        {
            if (obj == null)//|| obj is Lite)
                yield break;

            if (Reflector.IsMList(obj.GetType()))
            {
                Type t = obj.GetType().ElementType();
                if (Reflector.IsModifiableIdentifiableOrLite(t))
                {
                    IEnumerable col = obj as IEnumerable;
                    foreach (Modifiable item in col)
                        if (item != null)
                            yield return item;
                }
            }           
            else
            {
                foreach (Func<object, object> getter in ModifiableFieldGetters(obj.GetType()))
                {
                    object field = getter(obj);

                    if (field == null)
                        continue;

                    yield return (Modifiable)field;
                }

                IdentifiableEntity ident = obj as IdentifiableEntity;
                if (ident != null)
                {
                    foreach (var mixin in ident.AllMixin)
                    {
                        yield return mixin;
                    }
                }
            }
        }

        public static IEnumerable<Modifiable> IdentifiableExplore(Modifiable obj)
        {
            if (obj == null)//|| obj is Lite)
                yield break;

            if (Reflector.IsMList(obj.GetType()))
            {
                Type t = obj.GetType().ElementType();

                if (t.IsModifiable() && !t.IsIdentifiableEntity())
                {
                    IEnumerable col = obj as IEnumerable;
                    foreach (Modifiable item in col)
                        if (item != null)
                            yield return item;
                }
            }
            else
            {
                foreach (Func<object, object> getter in ModifiableFieldGetters(obj.GetType()))
                {
                    object field = getter(obj);

                    if (field == null || field is IdentifiableEntity)
                        continue;

                    yield return (Modifiable)field;
                }

                IdentifiableEntity ident = obj as IdentifiableEntity;
                if (ident != null)
                {
                    foreach (var mixin in ident.AllMixin)
                    {
                        yield return mixin;
                    }
                }
            }
        }
    }
}
