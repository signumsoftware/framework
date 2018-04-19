using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Signum.Utilities.Reflection;
using Signum.Utilities;

namespace Signum.Entities.Reflection
{

    public interface IEqualityComparerResolver
    {
        IEqualityComparer GetEqualityComparer(Type type, PropertyInfo pi);
    }

    public interface ICompletableComparer
    {
        void Complete(IEqualityComparerResolver resolver, PropertyInfo pi);
    }  

    public class EntityStructuralEqualityComparer<T> : EqualityComparer<T>, ICompletableComparer
        where T : ModifiableEntity
    {
        public Dictionary<string, PropertyComparer<T>> Properties;
        public Dictionary<Type, IEqualityComparer> Mixins;

        public EntityStructuralEqualityComparer()
        {
        }

        public void Complete(IEqualityComparerResolver resolver, PropertyInfo pi)
        { 
            Properties = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Typed | MemberOptions.Getter)
                .Where(p => !((PropertyInfo)p.MemberInfo).HasAttribute<HiddenPropertyAttribute>())
               .ToDictionary(a => a.Name, a => new PropertyComparer<T>((PropertyInfo)a.MemberInfo, a.Getter)
               {
                   Comparer = resolver.GetEqualityComparer(((PropertyInfo)a.MemberInfo).PropertyType, ((PropertyInfo)a.MemberInfo))
               });

            Mixins = !typeof(Entity).IsAssignableFrom(typeof(T)) ? null :
                MixinDeclarations.GetMixinDeclarations(typeof(T)).ToDictionary(t => t, t => resolver.GetEqualityComparer(t, null));
        }

        public override bool Equals(T x, T y)
        {
            if ((x == null) != (y == null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            foreach (var p in Properties.Values)
            {
                if (!p.Comparer.Equals(p.Getter(x), p.Getter(y)))
                    return false;
            }

            if (Mixins != null)
            {
                var ex = (Entity)(ModifiableEntity)x;
                var ey = (Entity)(ModifiableEntity)y;

                foreach (var kvp in Mixins)
                {
                    if (!kvp.Value.Equals(ex.GetMixin(kvp.Key), ey.GetMixin(kvp.Key)))
                        return false;
                }
            }

            return true;

        }

        public override int GetHashCode(T obj)
        {
            if (obj == null)
                return 0;

            int result = obj.GetType().GetHashCode();
            foreach (var p in Properties.Values)
            {
                result = result * 31 + p.Comparer.GetHashCode(p.Getter(obj));
            }

            if (Mixins != null)
            {
                var e = (Entity)(ModifiableEntity)obj;
                foreach (var kvp in Mixins)
                {
                    result = result * 31 + kvp.Value.GetHashCode(e.GetMixin(kvp.Key));
                }
            }

            return result;
        }
    }

    public class ClassStructuralEqualityComparer<T> : EqualityComparer<T>, ICompletableComparer
    {
        public Dictionary<string, PropertyComparer<T>> Properties;

        public ClassStructuralEqualityComparer()
        {
        }

        public void Complete(IEqualityComparerResolver resolver, PropertyInfo pi)
        {
            Properties = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Typed | MemberOptions.Getter)
              .ToDictionary(a => a.Name, a => new PropertyComparer<T>((PropertyInfo)a.MemberInfo, a.Getter)
              {
                  Comparer = resolver.GetEqualityComparer(((PropertyInfo)a.MemberInfo).PropertyType, ((PropertyInfo)a.MemberInfo))
              }); ;
        }

        public override bool Equals(T x, T y)
        {
            if ((x == null) != (y == null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            foreach (var p in Properties.Values)
            {
                if (!p.Comparer.Equals(p.Getter(x), p.Getter(y)))
                    return false;
            }

            return true;

        }

        public override int GetHashCode(T obj)
        {
            if (obj == null)
                return 0;

            int result = obj.GetType().GetHashCode();
            foreach (var p in Properties.Values)
            {
                result = result * 31 + p.Comparer.GetHashCode(p.Getter(obj));
            }

            return result;
        }


    }

    public class PropertyComparer<T>
    {
        public PropertyInfo MemberInfo { get; set; }
        public Func<T, object> Getter { get; set; }
        public IEqualityComparer Comparer { get; set; }

        public PropertyComparer(PropertyInfo memberInfo, Func<T, object> getter)
        {
            this.Getter = getter;
            this.MemberInfo = memberInfo;
        }

        public override string ToString() => $"{MemberInfo} -> {Comparer}";
    }

    public class SortedListEqualityComparer<T> : EqualityComparer<IList<T>>, ICompletableComparer
    {
        public IEqualityComparer<T> ElementComparer { get; set; }

        public SortedListEqualityComparer()
        {
        }
        
        public void Complete(IEqualityComparerResolver resolver, PropertyInfo pi)
        {
            ElementComparer = (IEqualityComparer<T>)resolver.GetEqualityComparer(typeof(T), pi);
        }

        public override bool Equals(IList<T> x, IList<T> y)
        {
            if ((x == null) != (y == null))
                return false;

            if (x.Count != y.Count)
                return false;

            for (int i = 0; i < x.Count; i++)
            {
                if (!ElementComparer.Equals(x[i], y[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode(IList<T> obj)
        {
            if (obj == null)
                return 0;

            int result = 17;
            foreach (var p in obj)
            {
                result = result * 31 + this.ElementComparer.GetHashCode(p);
            }

            return result;
        }
    }

    public class UnsortedListEqualityComparer<T> : EqualityComparer<IList<T>>, ICompletableComparer
    {
        public IEqualityComparer<T> ElementComparer { get; set; }

        public UnsortedListEqualityComparer()
        {
        }

        public void Complete(IEqualityComparerResolver resolver, PropertyInfo pi)
        {
            ElementComparer = (IEqualityComparer<T>)resolver.GetEqualityComparer(typeof(T), pi);
        }

        public override bool Equals(IList<T> mx, IList<T> my)
        {
            if ((mx == null) != (my == null))
                return false;

            if (mx.Count != my.Count)
                return false;

            var dic = mx.GroupToDictionary(x => ElementComparer.GetHashCode(x));
            foreach (var y in my)
            {
                var list = dic.TryGetC(ElementComparer.GetHashCode(y));

                if (list == null)
                    return false;

                var element = list.FirstOrDefault(l => ElementComparer.Equals(l, y));
                if (element == null)
                    return false;

                list.Remove(element);
            }

            return true;
        }

        public override int GetHashCode(IList<T> obj)
        {
            if (obj == null)
                return 0;

            int result = 0;
            foreach (var p in obj)
            {
                result += this.ElementComparer.GetHashCode(p);
            }

            return result;
        }
    }

    public abstract class EqualityComparerResolverWithCache : IEqualityComparerResolver
    {
        Dictionary<(Type, PropertyInfo), IEqualityComparer> cache = new Dictionary<(Type, PropertyInfo), IEqualityComparer>();
        public virtual IEqualityComparer GetEqualityComparer(Type type, PropertyInfo pi)
        {
            if (cache.TryGetValue((type, pi), out var comparer))
                return comparer;
            else
            {
                var comp = GetEqualityComparerInternal(type, pi);
                cache.Add((type, pi), comp);

                if (comp is ICompletableComparer cc)
                    cc.Complete(this, pi);

                return comp;
            }
        }

        protected abstract IEqualityComparer GetEqualityComparerInternal(Type type, PropertyInfo pi);
    }

    public class DefaultEqualityComparerResolver : EqualityComparerResolverWithCache
    {
        protected override IEqualityComparer GetEqualityComparerInternal(Type type, PropertyInfo pi)
        {
            if (typeof(ModifiableEntity).IsAssignableFrom(type))
            {
                return (IEqualityComparer)Activator.CreateInstance(typeof(EntityStructuralEqualityComparer<>).MakeGenericType(type));
            }
            
            if (typeof(IList).IsAssignableFrom(type))
            {
                if (pi?.HasAttribute<PreserveOrderAttribute>() == true)
                    return (IEqualityComparer)Activator.CreateInstance(typeof(SortedListEqualityComparer<>).MakeGenericType(type.ElementType()));
                else
                    return (IEqualityComparer)Activator.CreateInstance(typeof(UnsortedListEqualityComparer<>).MakeGenericType(type.ElementType()));
            }

            if (type.IsClass && type.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, null, new[] { typeof(object) }, null) == null)
                return (IEqualityComparer)Activator.CreateInstance(typeof(ClassStructuralEqualityComparer<>).MakeGenericType(type));
            
            return (IEqualityComparer)typeof(EqualityComparer<>).MakeGenericType(type).GetProperty("Default").GetValue(null);
        }
    }

}
