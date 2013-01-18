using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;

namespace Signum.Utilities
{
    public interface IResetLazy
    {
        void Reset();
        void Load();
    }

    [ComVisible(false)]
    [HostProtection(Action = SecurityAction.LinkDemand, Resources = HostProtectionResource.Synchronization | HostProtectionResource.SharedState)]
    public class ResetLazy<T>: IResetLazy where T : class
    {
        class Box
        {
            public Box(T value)
            {
                this.Value = value;
            }

            public readonly T Value;
        }

        public ResetLazy(Func<T> valueFactory, LazyThreadSafetyMode mode = LazyThreadSafetyMode.PublicationOnly)
        {
            if (valueFactory == null)
                throw new ArgumentNullException("valueFactory");

            this.mode = mode;
            this.valueFactory = valueFactory;
        }
        LazyThreadSafetyMode mode; 
        Func<T> valueFactory;

        object syncLock = new object();

        Box box;

        public T Value
        {
            get
            {
                var b1 = this.box;
                if (b1 != null)
                    return b1.Value;

                if (mode == LazyThreadSafetyMode.ExecutionAndPublication)
                {
                    lock (syncLock)
                    {
                        var b2 = box;
                        if (b2 != null)
                            return b2.Value;

                        this.box = new Box(valueFactory());

                        return box.Value;
                    }
                }

                else if (mode == LazyThreadSafetyMode.PublicationOnly)
                {
                    var newValue = valueFactory(); 

                    lock (syncLock)
                    {
                        var b2 = box;
                        if (b2 != null)
                            return b2.Value;

                        this.box = new Box(newValue);

                        return box.Value;
                    }
                }
                else
                {
                    var b = new Box(valueFactory());
                    this.box = b;
                    return b.Value;
                }
            }
        }


        public void Load()
        {
            var a = Value;
        }
       
        public bool IsValueCreated
        {
            get { return box != null; }
        }

        public void Reset()
        {
            if (mode != LazyThreadSafetyMode.None)
            {
                lock (syncLock)
                {
                    this.box = null;
                }
            }
            else
            {
                this.box = null;
            }
        }

        public Type DeclaredType
        {
            get
            {
                if (valueFactory == null) 
                    return null;

                return valueFactory.Method.DeclaringType;
            }
        }
    }
}
