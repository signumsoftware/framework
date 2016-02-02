using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities
{
    [Serializable]
    public class OperationSymbol : Symbol
    {
        private OperationSymbol() { } 

        private OperationSymbol(Type declaringType, string fieldName)
            : base(declaringType, fieldName)
        {
        }

        public static class Construct<T>
            where T : class, IEntity
        {
            public static ConstructSymbol<T>.Simple Simple(Type declaringType, string fieldName)
            {
                return new SimpleImp { Symbol = new OperationSymbol(declaringType, fieldName) };
            }
            
            public static ConstructSymbol<T>.From<F> From<F>(Type declaringType, string fieldName)
                where F : class,  IEntity
            {
                return new FromImp<F> { Symbol = new OperationSymbol(declaringType, fieldName) };
            }
            
            public static ConstructSymbol<T>.FromMany<F>  FromMany<F>(Type declaringType, string fieldName)
                where F : class, IEntity
            {
                return new FromManyImp<F> { Symbol = new OperationSymbol(declaringType, fieldName) };
            }

            [Serializable]
            class SimpleImp : ConstructSymbol<T>.Simple
            {
                OperationSymbol symbol;
                public OperationSymbol Symbol
                {
                    get { return symbol; }
                    internal set { this.symbol = value; }
                }

                public override string ToString()
                {
                    return "{0}({1})".FormatWith(this.GetType().TypeName(), Symbol);
                }
            }

            [Serializable]
            class FromImp<F> : ConstructSymbol<T>.From<F>
                where F : class, IEntity
            {
                OperationSymbol symbol;
                public OperationSymbol Symbol
                {
                    get { return symbol; }
                    internal set { this.symbol = value; }
                }

                public Type BaseType
                {
                    get { return typeof(F); }
                }

                public override string ToString()
                {
                    return "{0}({1})".FormatWith(this.GetType().TypeName(), Symbol);
                }
            }

            [Serializable]
            class FromManyImp<F> : ConstructSymbol<T>.FromMany<F>
                where F : class, IEntity
            {
                OperationSymbol symbol;
                public OperationSymbol Symbol
                {
                    get { return symbol; }
                    internal set { this.symbol = value; }
                }

                public Type BaseType
                {
                    get { return typeof(F); }
                }

                public override string ToString()
                {
                    return "{0}({1})".FormatWith(this.GetType().TypeName(), Symbol);
                }
            }
        }
        
        public static ExecuteSymbol<T> Execute<T>(Type declaringType, string fieldName)
            where T : class,  IEntity
        {
            return new ExecuteSymbolImp<T> { Symbol = new OperationSymbol(declaringType, fieldName) };
        }
        
        public static DeleteSymbol<T> Delete<T>(Type declaringType, string fieldName)
            where T : class, IEntity
        {
            return new DeleteSymbolImp<T> { Symbol = new OperationSymbol(declaringType, fieldName) };
        }
        
        [Serializable]
        class ExecuteSymbolImp<T> : ExecuteSymbol<T>
          where T : class, IEntity
        {
            OperationSymbol symbol;
            public OperationSymbol Symbol
            {
                get { return symbol; }
                internal set { this.symbol = value; }
            }

            public Type BaseType
            {
                get { return typeof(T); }
            }

            public override string ToString()
            {
                return "{0}({1})".FormatWith(this.GetType().TypeName(), Symbol);
            }
        }

        [Serializable]
        class DeleteSymbolImp<T> : DeleteSymbol<T>
          where T : class, IEntity
        {
            OperationSymbol symbol;
            public OperationSymbol Symbol
            {
                get { return symbol; }
                internal set { this.symbol = value; }
            }

            public Type BaseType
            {
                get { return typeof(T); }
            }

            public override string ToString()
            {
                return "{0}({1})".FormatWith(this.GetType().TypeName(), Symbol);
            }
        }
    }

    public interface IOperationSymbolContainer
    {
        OperationSymbol Symbol { get; }
    }

    public interface IEntityOperationSymbolContainer : IOperationSymbolContainer
    {
    }

    public interface IEntityOperationSymbolContainer<in T> : IEntityOperationSymbolContainer
        where T : class, IEntity
    {
        Type BaseType { get; }
    }

    public interface IConstructFromManySymbolContainer<in T> : IOperationSymbolContainer
       where T : class, IEntity
    {
        Type BaseType { get; }
    }


    public static class ConstructSymbol<T>
        where T : class, IEntity
    {
        public interface Simple : IOperationSymbolContainer
        {
        }

        public interface From<in F> : IEntityOperationSymbolContainer<F>
            where F : class, IEntity
        {
        }

        public interface FromMany<in F> : IConstructFromManySymbolContainer<F>
            where F : class, IEntity
        {
        }
    }

    public interface ExecuteSymbol<in T> : IEntityOperationSymbolContainer<T>
        where T : class, IEntity
    {
    }

    public interface DeleteSymbol<in T> : IEntityOperationSymbolContainer<T>
        where T : class, IEntity
    {
    }

    [Serializable]
    public class OperationInfo
    {
        public OperationSymbol OperationSymbol { get; internal set; }
        public OperationType OperationType { get; internal set; }

        public bool? Lite { get; internal set; }
        public bool? AllowsNew { get; internal set; }
        public bool? HasStates { get; internal set; }
        public bool? HasCanExecute { get; internal set; }

        public bool Returns { get; internal set; }
        public Type ReturnType { get; internal set; }
        public Type BaseType { get; internal set; }

        public override string ToString()
        {
            return "{0} ({1}) Lite = {2}, Returns {3}".FormatWith(OperationSymbol, OperationType, Lite, Returns);
        }

        public bool IsEntityOperation
        {
            get
            {
                return OperationType == OperationType.Execute ||
                    OperationType == OperationType.ConstructorFrom ||
                    OperationType == OperationType.Delete;
            }
        }
    }

    [InTypeScript(true)]
    public enum OperationType
    {
        Execute,
        Delete,
        Constructor,
        ConstructorFrom,
        ConstructorFromMany
    }
}
