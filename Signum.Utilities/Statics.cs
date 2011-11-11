using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Signum.Utilities
{
    public class Statics
    {
        static Dictionary<string, IUntypedVariable> threadVariables = new Dictionary<string, IUntypedVariable>();

        public static IVariable<T> ThreadVariable<T>(string name)
        {
            var variable = new ThreadLocalVariable<T>(name);
            threadVariables.AddOrThrow(name, variable, "Thread variable {0} already defined");
            return variable;
        }

        public static void CleanThread()
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

        class ThreadLocalVariable<T> : IVariable<T>
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


        static Dictionary<string, IUntypedVariable> sessionVariables = new Dictionary<string, IUntypedVariable>();

        public static IVariable<T> SessionVariable<T>(string name)
        {
            var variable = SessionFactory.CreateVariable<T>(name);
            sessionVariables.AddOrThrow(name, variable, "Session variable {0} already defined");
            return variable;
        }


        static ISessionFactory sessionFactory;
        public static ISessionFactory SessionFactory
        {
            get
            {
                if (sessionFactory == null)
                    throw new InvalidOperationException("Statics.SessionFactory not determined. (Tip: add a StaticSessionFactory, ScopeSessionFactory or AspNetSessionFactory)");

                return sessionFactory;
            }
            set { sessionFactory = value; }
        }
    }

    public interface IUntypedVariable
    {
        string Name { get; }
        object UntypedValue { get; set; }
        void Clean();
    }

    public interface IVariable<T> : IUntypedVariable
    {
        T Value { get; set; }
    }

    public interface ISessionFactory
    {
        IVariable<T> CreateVariable<T>(string name);
    }

    public class StaticSessionFactory : ISessionFactory
    {
        public IVariable<T> CreateVariable<T>(string name)
        {
            return new StaticVariable<T>(name);
        }

        class StaticVariable<T> : IVariable<T>
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

    public class ScopeSessionFactory : ISessionFactory
    {
        public ISessionFactory Factory;

        static IVariable<Dictionary<string, object>> overridenSession = Statics.ThreadVariable<Dictionary<string, object>>("overridenSession");

        public ScopeSessionFactory(ISessionFactory factory)
        {
            this.Factory = factory;
        }

        public static IDisposable OverrideSession()
        {
            return OverrideSession(new Dictionary<string, object>());
        }

        public static IDisposable OverrideSession(Dictionary<string, object> sessionDictionary)
        {
            var old = overridenSession.Value;
            overridenSession.Value = sessionDictionary;
            return new Disposable(() => overridenSession.Value = old);
        }

        public IVariable<T> CreateVariable<T>(string name)
        {
            return new OverrideableVariable<T>(Factory.CreateVariable<T>(name));
        }

        class OverrideableVariable<T> : IVariable<T>
        {
            IVariable<T> variable;

            public OverrideableVariable(IVariable<T> variable)
            {
                this.variable = variable;
            }

            public T Value
            {
                get
                {
                    var dic = overridenSession.Value;

                    if (dic != null)
                        return (T)(dic.TryGetC(variable.Name) ?? default(T));
                    else
                        return variable.Value;
                }
                set
                {
                    var dic = overridenSession.Value;

                    if (dic != null)
                        dic[variable.Name] = value;
                    else
                        variable.Value = value;
                }
            }

            public string Name
            {
                get { return variable.Name; }
            }

            public object UntypedValue
            {
                get { return this.Value; }
                set { this.Value = (T)value; }
            }

            public void Clean()
            {
                var dic = overridenSession.Value;

                if (dic != null)
                    dic.Remove(variable.Name);
                else
                    variable.Clean();
            }
        }
    }
}
