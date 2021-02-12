using System;
using Signum.Utilities;
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
                return new SimpleImp(new OperationSymbol(declaringType, fieldName));
            }

            public static ConstructSymbol<T>.From<F> From<F>(Type declaringType, string fieldName)
                where F : class,  IEntity
            {
                return new FromImp<F>(new OperationSymbol(declaringType, fieldName));
            }

            public static ConstructSymbol<T>.FromMany<F>  FromMany<F>(Type declaringType, string fieldName)
                where F : class, IEntity
            {
                return new FromManyImp<F>(new OperationSymbol(declaringType, fieldName));
            }

            [Serializable]
            class SimpleImp : ConstructSymbol<T>.Simple
            {
                public SimpleImp(OperationSymbol symbol)
                {
                    this.Symbol = symbol;
                }

                public OperationSymbol Symbol { get; internal set; }

                public override string ToString()
                {
                    return "{0}({1})".FormatWith(this.GetType().TypeName(), Symbol);
                }
            }

            [Serializable]
            class FromImp<F> : ConstructSymbol<T>.From<F>
                where F : class, IEntity
            {
                public FromImp(OperationSymbol symbol)
                {
                    Symbol = symbol;
                }

                public OperationSymbol Symbol { get; private set; }

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
                public FromManyImp(OperationSymbol symbol)
                {
                    Symbol = symbol;
                }

                public OperationSymbol Symbol { get; set; }

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
            return new ExecuteSymbolImp<T>(new OperationSymbol(declaringType, fieldName));
        }

        public static DeleteSymbol<T> Delete<T>(Type declaringType, string fieldName)
            where T : class, IEntity
        {
            return new DeleteSymbolImp<T>(new OperationSymbol(declaringType, fieldName));
        }

        [Serializable]
        class ExecuteSymbolImp<T> : ExecuteSymbol<T>
          where T : class, IEntity
        {
            public ExecuteSymbolImp(OperationSymbol symbol)
            {
                Symbol = symbol;
            }

            public OperationSymbol Symbol { get; private set; }
            
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
            public DeleteSymbolImp(OperationSymbol symbol)
            {
                Symbol = symbol;
            }

            public OperationSymbol Symbol { get; private set; }

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

        public interface FromMany<in F> : IOperationSymbolContainer
            where F : class, IEntity
        {
            Type BaseType { get; }
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
        public OperationInfo(OperationSymbol symbol, OperationType type)
        {
            this.OperationSymbol = symbol;
            this.OperationType = type;
        }


        public OperationSymbol OperationSymbol { get; internal set; }
        public OperationType OperationType { get; internal set; }

        public bool? CanBeModified { get; internal set; }
        public bool? CanBeNew { get; internal set; }
        public bool? HasStates { get; internal set; }
        public bool? HasCanExecute { get; internal set; }

        public bool Returns { get; internal set; }
        public Type? ReturnType { get; internal set; }
        public Type? BaseType { get; internal set; }

        public override string ToString()
        {
            return "{0} ({1})".FormatWith(OperationSymbol, OperationType);
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

    [InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
    public enum PropertyOperation
    {
        Set,
        AddElement,
        AddNewElement,
        ChangeElements,
        RemoveElement,
        RemoveElementsWhere,
        ModifyEntity,
        CreateNewEntity,
    }
}
