using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Principal;
using Signum.Utilities.Reflection;

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

        public static void AssertCleanThreadContext()
        {
            foreach (var item in threadVariables)
            {
                if (!item.Value.IsClean)
                    throw new InvalidOperationException("The thread variable '{0}' contains the non-default value '{1}'".Formato(item.Value.Name, item.Value.UntypedValue));
            }
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
            get { return Value == null || Value.Equals(typeof(T)); }
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
