using System;
using System.Collections.Generic;
using System.Text;


namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class NullableContextAttribute : Attribute
    {
        //0 : Dont't know
        //1 : Not nullable
        //2 : nullable
        public readonly byte[] NullableFlags;
        public NullableContextAttribute(byte flag)
        {
            NullableFlags = new byte[] { flag };
        }
        public NullableContextAttribute(byte[] flags)
        {
            NullableFlags = flags;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class NullableAttribute : Attribute
    {
        //0 : Dont't know
        //1 : Not nullable
        //2 : nullable
        public readonly byte[] NullableFlags;
        public NullableAttribute(byte flag)
        {
            NullableFlags = new byte[] { flag };
        }
        public NullableAttribute(byte[] flags)
        {
            NullableFlags = flags;
        }
    }
}
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AllowNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class DisallowNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
    public class MaybeNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
    public class NotNullAttribute : Attribute
    {
    }


    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
    public class MaybeNullWhenAttribute : Attribute
    {
        public bool ReturnValue { get; private set; }
        public MaybeNullWhenAttribute(bool returnValue)
        {
            this.ReturnValue = returnValue;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
    public class NotNullWhenAttribute : Attribute
    {
        public bool ReturnValue { get; private set; }
        public NotNullWhenAttribute(bool returnValue)
        {
            this.ReturnValue = returnValue;
        }
    }

    [AttributeUsage(AttributeTargets.Property |  AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
    public class NotNullIfNotNullAttribute : Attribute
    {
        public string Name { get; private set; }
        public NotNullIfNotNullAttribute(string name)
        {
            this.Name = name;
        }
    }
}
