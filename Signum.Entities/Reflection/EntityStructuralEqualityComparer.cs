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

    public class EntityStructuralEqualityComparer<T> : EqualityComparer<T> 
        where T : ModifiableEntity
    {
        Dictionary<string, PropertyComparer> Properties; 
        Dictionary<Type, IEqualityComparer> Mixins; 
        
        public EntityStructuralEqualityComparer(IEqualityComparerResolver resolver)
        {
            Properties = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Typed | MemberOptions.Getter)
               .ToDictionary(a => a.Name, a => new PropertyComparer((PropertyInfo)a.MemberInfo, a.Getter)
               {
                   Comparer = resolver.GetEqualityComparer(((PropertyInfo)a.MemberInfo).DeclaringType, ((PropertyInfo)a.MemberInfo))
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

            if(Mixins != null)
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

        public class PropertyComparer
        {
            public PropertyInfo MemberInfo { get; set; }
            public Func<T, object> Getter { get; set; }
            public IEqualityComparer Comparer { get; set; }

            public PropertyComparer(PropertyInfo memberInfo, Func<T, object> getter)
            {
                this.Getter = getter;
                this.MemberInfo = memberInfo;
            }
        }
    }

    public class SortedMListEqualityComparer<T> : EqualityComparer<MList<T>>
    {
        public IEqualityComparer<T> ElementComparer { get; set; }
        
        public SortedMListEqualityComparer(IEqualityComparerResolver resolver, PropertyInfo pi)
        {
            ElementComparer = (IEqualityComparer<T>)resolver.GetEqualityComparer(typeof(T), pi);
        }

        public override bool Equals(MList<T> x, MList<T> y)
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

        public override int GetHashCode(MList<T> obj)
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

    public class UnsortedMListEqualityComparer<T> : EqualityComparer<MList<T>>
    {
        public IEqualityComparer<T> ElementComparer { get; set; }

        public UnsortedMListEqualityComparer(IEqualityComparerResolver resolver, PropertyInfo pi)
        {
            ElementComparer = (IEqualityComparer<T>)resolver.GetEqualityComparer(typeof(T), pi);
        }

        public override bool Equals(MList<T> mx, MList<T> my)
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

        public override int GetHashCode(MList<T> obj)
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

    public class DefaultEqualityComparerResolver : IEqualityComparerResolver
    {
        public IEqualityComparer GetEqualityComparer(Type type, PropertyInfo pi)
        {
            if (typeof(EmbeddedEntity).IsAssignableFrom(type) ||
                typeof(MixinEntity).IsAssignableFrom(type) ||
                typeof(ModelEntity).IsAssignableFrom(type))
            {
                return (IEqualityComparer)Activator.CreateInstance(typeof(EntityStructuralEqualityComparer<>).MakeGenericType(type), this);
            }

            if(typeof(IMListPrivate).IsAssignableFrom(type))
            {
                if(pi?.HasAttribute<PreserveOrderAttribute>() == true)
                    return (IEqualityComparer)Activator.CreateInstance(typeof(SortedMListEqualityComparer<>).MakeGenericType(type), this, pi);
                else
                    return (IEqualityComparer)Activator.CreateInstance(typeof(UnsortedMListEqualityComparer<>).MakeGenericType(type), this, pi);
            }

            return EqualityComparer<object>.Default;
        }
    }
}
