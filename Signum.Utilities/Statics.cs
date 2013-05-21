using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Principal;
using Signum.Utilities.Reflection;
using System.Collections;

namespace Signum.Utilities
{
    public class Statics
    {
        static Dictionary<string, IUntypedVariable> threadVariables = new Dictionary<string, IUntypedVariable>();

        public static ThreadVariable<T> ThreadVariable<T>(string name)
        {
            var variable = new ThreadVariable<T>(name);
            threadVariables.AddOrThrow(name, variable, "Thread variable {0} already defined");
            return variable;
        }
       
        public static Dictionary<string, object> ExportThreadContext()
        {
            return threadVariables.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.UntypedValue);
        }

        public static IDisposable SetThreadContext(Dictionary<string, object> context)
        {
            foreach (var kvp in context)
            {
                threadVariables[kvp.Key].UntypedValue = kvp.Value;
            }

            return new Disposable(() =>
            {
                foreach (var v in threadVariables.Values)
                {
                    v.Clean();
                }
            });
        }

        public static void CleanThreadContextAndAssert()
        {
            string errors = threadVariables.Values.Where(v => !v.IsClean).ToString(v => "{0} contains the non-default value {1}".Formato(v.Name, v.UntypedValue), "\r\n");

            foreach (var v in threadVariables.Values)
            {
                v.Clean();
            }

            if (errors.HasText())
                throw new InvalidOperationException("The thread variable \r\n" + errors);
        }

        static Dictionary<string, IUntypedVariable> sessionVariables = new Dictionary<string, IUntypedVariable>();

        public static SessionVariable<T> SessionVariable<T>(string name)
        {
            var variable = SessionFactory.CreateVariable<T>(name);
            sessionVariables.AddOrThrow(name, variable, "Session variable {0} already defined");
            return variable;
        }

        static ISessionFactory sessionFactory = new ScopeSessionFactory(new SingletonSessionFactory());
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
        bool IsClean {get;}
        void Clean();
    }

    public abstract class Variable<T> : IUntypedVariable
    {
        public string Name { get; private set; }

        public Variable(string name)
        {
            this.Name = name;
        }

        public abstract T Value { get; set; }

        public object UntypedValue
        {
            get { return Value; }
            set { Value = (T)value; }
        }

        public bool IsClean
        {
            get
            {
                if (Value == null)
                    return true;

                if (Value.Equals(default(T)))
                    return true;

                var col = Value as IEnumerable;

                if (col != null)
                {
                    foreach (var item in col)
                    {
                        return false;
                    }

                    return true;
                }

                return false;
            }
        }

        public abstract void Clean();
    }

    public class ThreadVariable<T> : Variable<T>
    {
        ThreadLocal<T> store = new ThreadLocal<T>();

        public ThreadVariable(string name) : base(name) { }

        public override T Value
        {
            get { return store.Value; }
            set { store.Value = value; }
        }

        public override void Clean()
        {
            Value = default(T);
        }
    }

    public abstract class SessionVariable<T>: Variable<T>
    {
        public abstract Func<T> ValueFactory { get; set; }

        public SessionVariable(string name)
            : base(name)
        {
        }

        public T GetDefaulValue()
        {
            if (ValueFactory == null)
                return default(T);

            Value = ValueFactory();

            return Value;
        }
    }


    public interface ISessionFactory
    {
        SessionVariable<T> CreateVariable<T>(string name);
    }

    public class SingletonSessionFactory : ISessionFactory
    {
        public static Dictionary<string, object> singletonSession = new Dictionary<string, object>();

        public static Dictionary<string, object> SinglestonSession
        {
            get { return singletonSession; }
            set { singletonSession = value; }
        }

        public SessionVariable<T> CreateVariable<T>(string name)
        {
            return new SingletonVariable<T>(name);
        }

        class SingletonVariable<T> : SessionVariable<T>
        {
            public override Func<T> ValueFactory { get; set; }
            
            public SingletonVariable(string name)
                : base(name)
            { }

            public override T Value
            {
                get
                {
                    object result;
                    if (singletonSession.TryGetValue(Name, out result))
                        return (T)result;

                    return GetDefaulValue();
                }
                set { singletonSession[Name] = value; }
            }

            public override void Clean()
            {
                singletonSession.Remove(Name); 
            }
        }
    }

    public class ScopeSessionFactory : ISessionFactory
    {
        public ISessionFactory Factory;

        static readonly ThreadVariable<Dictionary<string, object>> overridenSession = Statics.ThreadVariable<Dictionary<string, object>>("overridenSession");

        public static bool IsOverriden
        {
            get { return overridenSession.Value != null; }
        }

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
            if (!(Statics.SessionFactory is ScopeSessionFactory))
                throw new InvalidOperationException("Impossible to OverrideSession because Statics.SessionFactory is not a ScopeSessionFactory"); 
            var old = overridenSession.Value;
            overridenSession.Value = sessionDictionary;
            return new Disposable(() => overridenSession.Value = old);
        }

        public SessionVariable<T> CreateVariable<T>(string name)
        {
            return new OverrideableVariable<T>(Factory.CreateVariable<T>(name));
        }

        class OverrideableVariable<T> : SessionVariable<T>
        {
            SessionVariable<T> variable;

            public OverrideableVariable(SessionVariable<T> variable)
                : base(variable.Name)
            {
                this.variable = variable;
            }

            public override Func<T> ValueFactory
            {
                get { return variable.ValueFactory; }
                set { variable.ValueFactory = value; }
            }

            public override T Value
            {
                get
                {
                    var dic = overridenSession.Value;

                    if (dic != null)
                    {
                        object result;
                        if (dic.TryGetValue(Name, out result))
                            return (T)result;

                        return GetDefaulValue();
                    }
                    else
                        return variable.Value;
                }
                set
                {
                    var dic = overridenSession.Value;

                    if (dic != null)
                        dic[Name] = value;
                    else
                        variable.Value = value;
                }
            }

            public override void Clean()
            {
                var dic = overridenSession.Value;

                if (dic != null)
                    dic.Remove(Name);
                else
                    variable.Clean();
            }
        }
    }
}
