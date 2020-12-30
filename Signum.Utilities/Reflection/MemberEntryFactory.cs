using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;

namespace Signum.Utilities.Reflection
{
    public static class MemberEntryFactory
    {
        static BindingFlags bf = BindingFlags.Public | BindingFlags.Instance;

        public static List<MemberEntry<T>> GenerateList<T>(MemberOptions options, Type? type = null)
        {
            if (type != null && !typeof(T).IsAssignableFrom(type))
                throw new InvalidOperationException($"Type {type} is assignable {typeof(T)}");

            var finalType = type ?? typeof(T); 

            PropertyInfo[] properties = (options & MemberOptions.Properties) == 0 ? new PropertyInfo[0] : typeof(T).GetProperties(bf);
            FieldInfo[] fields = (options & MemberOptions.Fields) == 0 ? new FieldInfo[0] : typeof(T).GetFields(bf);

            var members = properties.Where(p => p.GetIndexParameters().Length == 0).Cast<MemberInfo>()
                .Concat(fields.Cast<MemberInfo>()).OrderBy(e => e.MetadataToken).ToArray();

            var result = members.Select(m => new MemberEntry<T>(
                m.Name, m,
                options.IsSet(MemberOptions.Getter) ? ReflectionTools.CreateGetter<T, object?>(m) : null,
                options.IsSet(MemberOptions.Setters) ? ReflectionTools.CreateSetter<T, object?>(m) : null
                )).ToList();

            return result;
        }

     
        static bool IsSet(this MemberOptions options, MemberOptions flags)
        {
            return (options & flags) == flags; 
        }
    }

    //Each pair of flags sets an ortogonal option
    [Flags]
    public enum MemberOptions
    {
        Properties = 1, 
        Fields = 2,

        Getter = 4,
        Setters = 8, 

        Default = Properties|Fields|Getter,
    }

    public class MemberEntry<T>
    {
        public string Name {get; private set;}
        public MemberInfo MemberInfo { get; private set; }
        public Func<T, object?>? Getter { get; private set; }
        public Action<T, object?>? Setter { get; private set; }

        public MemberEntry(string name, MemberInfo memberInfo, Func<T, object?>? getter, Action<T, object?>? setter)
        {
            this.Name = name;
            this.MemberInfo = memberInfo;

            this.Getter = getter;
            this.Setter = setter;
        }
    }

    public interface IMemberEntry
    {
        string Name { get; }
        MemberInfo MemberInfo { get; }
        Func<object, object?>? UntypedGetter { get; }
        Action<object, object?>? UntypedSetter { get; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    sealed class OrderAttribute : Attribute
    {
        readonly int order;
        public int Order
        {
            get { return order; }
        }

        public OrderAttribute(int order)
        {
            this.order = order;
        }
    }
}
