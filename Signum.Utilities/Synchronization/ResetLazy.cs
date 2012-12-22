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

        bool valueCreated = false; 
        T value;

        public T Value
        {
            get
            {
                if (valueCreated)
                    return value;

                if (mode == LazyThreadSafetyMode.ExecutionAndPublication)
                {
                    lock (syncLock)
                    {
                        if (valueCreated)
                            return value;

                        Set(valueFactory());
                    }
                }

                else if (mode == LazyThreadSafetyMode.PublicationOnly)
                {
                    var newValue = valueFactory(); 

                    lock (syncLock)
                    {
                        if (valueCreated)
                            return value;

                        Set(newValue);
                    }
                }
                else
                {
                    Set(valueFactory());
                }

                return value;
                
            }
        }

        void Set(T value)
        {
            this.valueCreated = true;
            this.value = value;
        }

        public void Load()
        {
            var a = Value;
        }
       
        public bool IsValueCreated
        {
            get { return valueCreated; }
        }

        public void Reset()
        {
            if (mode != LazyThreadSafetyMode.None)
            {
                lock (syncLock)
                {
                    this.value = null;
                    this.valueCreated = false;
                }
            }
            else
            {
                this.value = null;
                this.valueCreated = false;
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
