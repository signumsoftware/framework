using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Signum.Utilities
{
    public class Statics
    {
        static Dictionary<string, IUntypedContextVariable> threadVariables = new Dictionary<string, IUntypedContextVariable>(); 

        IContextVariable<T> ThreadVariable<T>(string name)
        {
            var variable = new ThreadLocalVariable<T>(name);
            threadVariables.AddOrThrow(name,variable , "Thread variable {0} already defined");
            return variable;
        }

        public static void CleanThread(string name)
        {
            threadVariables.Clear(); 
        }

        public static Dictionary<string, object> ExportThreadContext()
        {
            return threadVariables.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.UntypedValue);
        }

        public static void ImportThreadContext(Dictionary<string, object> context)
        {
            foreach (var kvp in context)
            {
                threadVariables[kvp.Key].UntypedValue = kvp.Value;
            }
        }

        class ThreadLocalVariable<T> : IContextVariable<T>
        {
            string name;
            public string Name { get { return name; } }
            ThreadLocal<T> store = new ThreadLocal<T>(); 

            public ThreadLocalVariable(string name)
            {
                this.name = name;
            }

            public T Value
            {
                get { return store.Value; }
                set { store.Value = value; }
            }

            public object UntypedValue
            {
                get { return store.Value; }
                set { store.Value = (T)value; }
            }

            public void Clean()
            {
                store.Value = default(T); 
            }
        }

        static Dictionary<string, IUntypedContextVariable> sessionVariables = new Dictionary<string, IUntypedContextVariable>();

        IContextVariable<T> SessionVariable<T>(string name)
        {
            var variable = new ThreadLocalVariable<T>(name);
            sessionVariables.AddOrThrow(name, variable, "Session variable {0} already defined");
            return variable;
        }

        public static ISessionVariableFactory SessionFactory { get; set; }
    }

    public interface IUntypedContextVariable
    {
        string Name { get; }
        object UntypedValue { get; set; }
        void Clean();
    }

    public interface IContextVariable<T>:IUntypedContextVariable
    {
        public T Value {get; set;}
    }

    interface ISessionVariableFactory 
    {
        IContextVariable<T> CreateVariable<T>(string name);
    }

    public class StaticVariableFactory : ISessionVariableFactory
    {
        public IContextVariable<T> CreateVariable<T>(string name)
        {
            return new StaticVariable<T>(name); 
        }

        class StaticVariable<T> : IContextVariable<T>
        {
            string name;
            public string Name { get { return name; } }
            T store;

            public StaticVariable(string name)
            {
                this.name = name;
            }

            public T Value
            {
                get { return store; }
                set { store = value; }
            }

            public object UntypedValue
            {
                get { return store; }
                set { store = (T)value; }
            }

            public void Clean()
            {
                store = default(T);
            }
        }
    }
}
