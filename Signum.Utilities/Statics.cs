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

        public static Variable<T> ThreadVariable<T>(string name)
        {
            var variable = new ThreadLocalVariable<T>(name);
            threadVariables.AddOrThrow(name, variable, "Thread variable {0} already defined");
            return variable;
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

        class ThreadLocalVariable<T> : Variable<T>
        {
            ThreadLocal<T> store = new ThreadLocal<T>();

            public ThreadLocalVariable(string name): base(name){}

            public override T Value
            {
                get { return store.Value; }
                set { store.Value = value; }
            }
        }

        static Dictionary<string, IUntypedVariable> sessionVariables = new Dictionary<string, IUntypedVariable>();

        public static Variable<T> SessionVariable<T>(string name)
        {
            var variable = SessionFactory.CreateVariable<T>(name);
            sessionVariables.AddOrThrow(name, variable, "Session variable {0} already defined");
            return variable;
        }

        static ISessionFactory sessionFactory = new StaticSessionFactory();
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

        public void Clean()
        {
            Value = default(T);
        }
    }

    public interface ISessionFactory
    {
        Variable<T> CreateVariable<T>(string name);
    }

    public class StaticSessionFactory : ISessionFactory
    {
        public Variable<T> CreateVariable<T>(string name)
        {
            return new StaticVariable<T>(name);
        }

        class StaticVariable<T> : Variable<T>
        {
            public StaticVariable(string name)
                : base(name)
            {
            }

            public override T Value { get; set; }
        }
    }

    public class ScopeSessionFactory : ISessionFactory
    {
        public ISessionFactory Factory;

        static readonly Variable<Dictionary<string, object>> overridenSession = Statics.ThreadVariable<Dictionary<string, object>>("overridenSession");

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

        public Variable<T> CreateVariable<T>(string name)
        {
            return new OverrideableVariable<T>(Factory.CreateVariable<T>(name));
        }

        class OverrideableVariable<T> : Variable<T>
        {
            Variable<T> variable;

            public OverrideableVariable(Variable<T> variable) : base(variable.Name)
            {
                this.variable = variable;
            }

            public override T Value
            {
                get
                {
                    var dic = overridenSession.Value;

                    if (dic != null)
                        return (T)(dic.TryGetC(Name) ?? default(T));
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
        }

        class ThrowSessionFactory : ISessionFactory
        {
            public Variable<T> CreateVariable<T>(string name)
            {
                return new ThrowVariable<T>(name);
            }

            class ThrowVariable<T> : Variable<T>
            {
                public ThrowVariable(string name) : base(name) { }

                public override T Value
                {
                    get { throw NoSession(); }
                    set { throw NoSession(); }
                }

                private static InvalidOperationException NoSession()
                {
                    return new InvalidOperationException("Session variables are not available. Call ScopeSessionFactory.Scope or determine an inner ScopeFactory");
                }
            }
        }
    }
}
