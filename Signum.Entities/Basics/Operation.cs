using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Signum.Entities
{
    [Serializable]
    public class OperationSymbol : Symbol
    {
        private OperationSymbol() { } 

        private OperationSymbol(StackFrame frame, string memberName)
            : base(frame, memberName)
        {
        }

        public static class Construct<T>
            where T : class, IIdentifiable
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static ConstructSymbol<T>.Simple Simple([CallerMemberName]string memberName = null)
            {
                return new SimpleImp { Operation = new OperationSymbol(new StackFrame(1, false), memberName) };
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static ConstructSymbol<T>.From<F> From<F>([CallerMemberName]string memberName = null)
                where F : class,  IIdentifiable
            {
                return new FromImp<F> { Operation = new OperationSymbol(new StackFrame(1, false), memberName) };
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static ConstructSymbol<T>.FromMany<F>  FromMany<F>([CallerMemberName]string memberName = null)
                where F : class, IIdentifiable
            {
                return new FromManyImp<F> { Operation = new OperationSymbol(new StackFrame(1, false), memberName) };
            }

            class SimpleImp : ConstructSymbol<T>.Simple
            {
                OperationSymbol operation;
                public OperationSymbol Operation
                {
                    get { return operation; }
                    internal set { this.operation = value; }
                }
            }

            class FromImp<F> : ConstructSymbol<T>.From<F>
                where F : class, IIdentifiable
            {
                OperationSymbol operation;
                public OperationSymbol Operation
                {
                    get { return operation; }
                    internal set { this.operation = value; }
                }
            }


            class FromManyImp<F> : ConstructSymbol<T>.FromMany<F>
                where F : class, IIdentifiable
            {
                OperationSymbol operation;
                public OperationSymbol Operation
                {
                    get { return operation; }
                    internal set { this.operation = value; }
                }
            }
        }

       

   

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ExecuteSymbol<T> Execute<T>([CallerMemberName]string memberName = null)
            where T : class,  IIdentifiable
        {
            return new ExecuteSymbolImp<T> { Operation = new OperationSymbol(new StackFrame(1, false), memberName) };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static DeleteSymbol<T> Delete<T>([CallerMemberName]string memberName = null)
            where T : class, IIdentifiable
        {
            return new DeleteSymbolImp<T> { Operation = new OperationSymbol(new StackFrame(1, false), memberName) };
        }

      

        class ExecuteSymbolImp<T> : ExecuteSymbol<T>
          where T : class, IIdentifiable
        {
            OperationSymbol operation;
            public OperationSymbol Operation
            {
                get { return operation; }
                internal set { this.operation = value; }
            }
        }

        class DeleteSymbolImp<T> : DeleteSymbol<T>
          where T : class, IIdentifiable
        {
            OperationSymbol operation;
            public OperationSymbol Operation
            {
                get { return operation; }
                internal set { this.operation = value; }
            }
        }

        public static string NotDefinedForMessage(OperationSymbol operation, IEnumerable<Type> notDefined)
        {
            if (notDefined.Any())
                return "{0} is not defined for {1}".Formato(operation.NiceToString(), notDefined.CommaAnd(a => a.NiceName()));

            return null;
        }
    }

    public interface IOperationSymbolContainer
    {
        OperationSymbol Operation { get; }
    }

    public interface IEntityOperationSymbolContainer<in T> : IOperationSymbolContainer
        where T : class, IIdentifiable
    {
    }

    public static class ConstructSymbol<T>
        where T : class, IIdentifiable
    {
        public interface Simple : IOperationSymbolContainer
        {
        }

        public interface From<in F> : IEntityOperationSymbolContainer<F>
            where F : class, IIdentifiable
        {
        }

        public interface FromMany<in F> : IOperationSymbolContainer
            where F : class, IIdentifiable
        {
        }
    }


    public interface ExecuteSymbol<in T> : IEntityOperationSymbolContainer<T>
        where T : class, IIdentifiable
    {
    }

    public interface DeleteSymbol<in T> : IEntityOperationSymbolContainer<T>
        where T : class, IIdentifiable
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

        public override string ToString()
        {
            return "{0} ({1}) Lite = {2}, Returns {3}".Formato(OperationSymbol, OperationType, Lite, Returns);
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

    [Flags]
    public enum OperationType
    {
        Execute,
        Delete,
        Constructor,
        ConstructorFrom,
        ConstructorFromMany
    }
}
