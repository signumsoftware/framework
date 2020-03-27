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
