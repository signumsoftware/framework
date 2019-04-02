using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;

namespace Signum.Utilities.Reflection
{
    public static class MemberEntryFactory
    {
        //Each element in the IList is a MemberEntry<T> with typeof(T) == type. 
        //You can acces them in a untypef fashion using IMemberEntry 
        public static IList GenerateIList(Type type)
        {
            return GenerateIList(type, MemberOptions.Default);
        }

        //Each element in the IList is a MemberEntry<T> with typeof(T) == type. 
        //You can acces them in a untyped fashion using IMemberEntry 
        public static IList GenerateIList(Type type, MemberOptions options)
        {
            return mi1.GetInvoker(type).Invoke(options);
        }

        public static List<MemberEntry<T>> GenerateList<T>()
        {
            return GenerateList<T>(MemberOptions.Default);
        }

        static BindingFlags bf = BindingFlags.Public | BindingFlags.Instance;

        static GenericInvoker<Func<MemberOptions, IList>> mi1 = new GenericInvoker<Func<MemberOptions, IList>>(mo => GenerateList<int>(mo));
        public static List<MemberEntry<T>> GenerateList<T>(MemberOptions options)
        {
            PropertyInfo[] properties = (options & MemberOptions.Properties) == 0 ? new PropertyInfo[0] : typeof(T).GetProperties(bf);
            FieldInfo[] fields = (options & MemberOptions.Fields) == 0 ? new FieldInfo[0] : typeof(T).GetFields(bf);

            var members = properties.Where(p => p.GetIndexParameters().Length == 0).Cast<MemberInfo>()
                .Concat(fields.Cast<MemberInfo>()).OrderBy(e => e.MetadataToken).ToArray();

            var result = members.Select(m => new MemberEntry<T>(
                m.Name, m,
                options.IsSet(MemberOptions.Getter | MemberOptions.Typed) ? ReflectionTools.CreateGetter<T>(m) : null,
                options.IsSet(MemberOptions.Getter | MemberOptions.Untyped) ? ReflectionTools.CreateGetterUntyped(typeof(T), m) : null,
                options.IsSet(MemberOptions.Setters | MemberOptions.Typed) ? ReflectionTools.CreateSetter<T>(m) : null,
                options.IsSet(MemberOptions.Setters | MemberOptions.Untyped) ? ReflectionTools.CreateSetterUntyped(typeof(T), m) : null
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

        Typed = 16,
        Untyped = 32,


        Default = Properties|Fields|Getter|Typed,
    }

    public class MemberEntry<T> : IMemberEntry
    {
        public string Name {get; private set;}
        public MemberInfo MemberInfo { get; private set; }
        public Func<T, object?>? Getter { get; private set; }
        public Func<object, object?>? UntypedGetter { get; private set; }
        public Action<T, object?>? Setter { get; private set; }
        public Action<object, object?>? UntypedSetter { get; private set; }

        public MemberEntry(string name, MemberInfo memberInfo, Func<T, object?>? getter, Func<object, object?>? untypedGetter, Action<T, object?>? setter, Action<object, object?>? untypedSetter)
        {
            this.Name = name;
            this.MemberInfo = memberInfo;

            this.Getter = getter;
            this.UntypedGetter = untypedGetter;

            this.Setter = setter;
            this.UntypedSetter = untypedSetter;
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
