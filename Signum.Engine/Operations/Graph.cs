using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Basics;
using Signum.Engine.Operations.Internal;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Operations
{
    public delegate F Overrider<F>(F baseFunc); 

    public class Graph<T>
         where T : class, IEntity
    {
        public class Construct : _Construct<T>, IConstructOperation
        {
            protected readonly OperationSymbol operationSymbol;
            OperationSymbol IOperation.OperationSymbol { get { return operationSymbol; } }
            Type IOperation.OverridenType { get { return typeof(T); } }
            OperationType IOperation.OperationType { get { return OperationType.Constructor; } }
            bool IOperation.Returns { get { return true; } }
            Type IOperation.ReturnType { get { return typeof(T); } }
            IEnumerable<Enum> IOperation.UntypedFromStates { get { return null; } }
            IEnumerable<Enum> IOperation.UntypedToStates { get { return Enumerable.Empty<Enum>(); } }
            Type IOperation.StateType { get { return null; } } 

            public bool LogAlsoIfNotSaved { get; set; }

            //public Func<object[], T> Construct { get; set; } (inherited)
            public bool Lite { get { return false; } }

            public Construct(ConstructSymbol<T>.Simple symbol)
            {
                if (symbol == null)
                    throw AutoInitAttribute.ArgumentNullException(typeof(ConstructSymbol<T>.Simple), nameof(symbol));

                this.operationSymbol = symbol.Symbol;
            }

            protected Construct(OperationSymbol operationSymbol)
            {
                this.operationSymbol = operationSymbol ?? throw new ArgumentNullException(nameof(operationSymbol));
            }

            public static Construct Untyped<B>(ConstructSymbol<B>.Simple symbol)
                where B: class, IEntity
            {
                return new Construct(symbol.Symbol);
            }

            public void OverrideConstruct(Overrider<Func<object[], T>> overrider)
            {
                this.Construct = overrider(this.Construct);
            }

            IEntity IConstructOperation.Construct(params object[] args)
            {
                using (HeavyProfiler.Log("Construct", () => operationSymbol.Key))
                {
                    OperationLogic.AssertOperationAllowed(operationSymbol, typeof(T), inUserInterface: false);

                    OperationLogEntity log = new OperationLogEntity
                    {
                        Operation = operationSymbol,
                        Start = TimeZoneManager.Now,
                        User = UserHolder.Current?.ToLite()
                    };

                    try
                    {
                        using (Transaction tr = new Transaction())
                        { 
                            T result = null;
                            using (OperationLogic.AllowSave<T>())
                                OperationLogic.OnSuroundOperation(this, log, null, args).EndUsing(_ =>
                                {
                                    result = Construct(args);

                                    AssertEntity(result);

                                    if ((result != null && !result.IsNew) || LogAlsoIfNotSaved)
                                    {
                                        log.SetTarget(result);
                                        log.End = TimeZoneManager.Now;
                                    }
                                    else
                                        log = null;
                                });

                            if (log != null)
                                log.SaveLog();

                            return tr.Commit(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        OperationLogic.SetExceptionData(ex, operationSymbol, null, args);

                        if (LogAlsoIfNotSaved)
                        {
                            if (Transaction.InTestTransaction)
                                throw;

                            var exLog = ex.LogException();

                            using (Transaction tr2 = Transaction.ForceNew())
                            {
                                log.Exception = exLog.ToLite();

                                log.SaveLog();

                                tr2.Commit();
                            }
                        }

                        throw;
                    }
                }
            }

       

            protected virtual void AssertEntity(T entity)
            {
            }

            public virtual void AssertIsValid()
            {
                if (Construct == null)
                    throw new InvalidOperationException("Operation {0} does not have Constructor initialized".FormatWith(operationSymbol));
            }

            public override string ToString()
            {
                return "{0} Construct {1}".FormatWith(operationSymbol, typeof(T));
            }
        }

        public class ConstructFrom<F> : IConstructorFromOperation
            where F : class, IEntity
        {
            protected readonly OperationSymbol operationSymbol;
            OperationSymbol IOperation.OperationSymbol { get { return operationSymbol; } }
            Type IOperation.OverridenType { get { return typeof(F); } }
            OperationType IOperation.OperationType { get { return OperationType.ConstructorFrom; } }
            IEnumerable<Enum> IOperation.UntypedFromStates { get { return null; } }
            IEnumerable<Enum> IOperation.UntypedToStates { get { return Enumerable.Empty<Enum>(); } }
            Type IOperation.StateType { get { return null; } }

            public bool Lite { get; set; } = true;
            public bool LogAlsoIfNotSaved { get; set; }

            bool IOperation.Returns { get { return true; } }
            Type IOperation.ReturnType { get { return typeof(T); } }

            protected readonly Type baseType;
            Type IEntityOperation.BaseType { get { return baseType; } }
            bool IEntityOperation.HasCanExecute { get { return CanConstruct != null; } }

            public bool AllowsNew { get; set; }

            public Func<F, string> CanConstruct { get; set; }

            public ConstructFrom<F> OverrideCanConstruct(Overrider<Func<F, string>> overrider)
            {
                this.CanConstruct = overrider(this.CanConstruct ?? (f => null));
                return this;
            }

            public Func<F, object[], T> Construct { get; set; }

            public void OverrideConstruct(Overrider<Func<F, object[], T>> overrider)
            {
                this.Construct = overrider(this.Construct);
            }

            public ConstructFrom(ConstructSymbol<T>.From<F> symbol)
            {
                if (symbol == null)
                    throw AutoInitAttribute.ArgumentNullException(typeof(ConstructSymbol<T>.From<F>), nameof(symbol));

                this.operationSymbol = symbol.Symbol;
                this.baseType = symbol.BaseType;
            }

            protected ConstructFrom(OperationSymbol operationSymbol, Type baseType)
            {
                this.operationSymbol = operationSymbol ?? throw new ArgumentNullException(nameof(operationSymbol));
                this.baseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
            }

            public static ConstructFrom<F> Untyped<B>(ConstructSymbol<B>.From<F> symbol)
                where B : class, IEntity
            {
                return new ConstructFrom<F>(symbol.Symbol, symbol.BaseType);
            }

            string IEntityOperation.CanExecute(IEntity entity)
            {
                return OnCanConstruct(entity);
            }

            string OnCanConstruct(IEntity entity)
            {
                if (entity.IsNew && !AllowsNew)
                    return EngineMessage.TheEntity0IsNew.NiceToString().FormatWith(entity);

                if (CanConstruct != null)
                    return CanConstruct((F)entity);

                return null;
            }

            IEntity IConstructorFromOperation.Construct(IEntity origin, params object[] args)
            {
                using (HeavyProfiler.Log("ConstructFrom", () => operationSymbol.Key))
                {
                    OperationLogic.AssertOperationAllowed(operationSymbol, origin.GetType(), inUserInterface: false);

                    string error = OnCanConstruct(origin);
                    if (error != null)
                        throw new ApplicationException(error);

                    OperationLogEntity log = new OperationLogEntity
                    {
                        Operation = operationSymbol,
                        Start = TimeZoneManager.Now,
                        User = UserHolder.Current?.ToLite(),
                        Origin = origin.ToLite(origin.IsNew),
                    };

                    try
                    {
                        using (Transaction tr = new Transaction())
                        {
                            T result = null;
                            using (OperationLogic.AllowSave(origin.GetType()))
                            using (OperationLogic.AllowSave<T>())
                                OperationLogic.OnSuroundOperation(this, log, origin, args).EndUsing(_ =>
                                {
                                    result = Construct((F)origin, args);

                                    AssertEntity(result);

                                    if ((result != null && !result.IsNew) || LogAlsoIfNotSaved)
                                    {
                                        log.End = TimeZoneManager.Now;
                                        log.SetTarget(result);
                                    }
                                    else
                                    {
                                        log = null;
                                    }
                                });

                            if (log != null)
                                log.SaveLog();

                            return tr.Commit(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        OperationLogic.SetExceptionData(ex, operationSymbol, (Entity)origin, args);

                        if (LogAlsoIfNotSaved)
                        {
                            if (Transaction.InTestTransaction)
                                throw;

                            var exLog = ex.LogException();

                            using (Transaction tr2 = Transaction.ForceNew())
                            {
                                log.Exception = exLog.ToLite();

                                log.SaveLog();

                                tr2.Commit();
                            }
                        }

                        throw;
                    }

                }
            }

            protected virtual void AssertEntity(T entity)
            {
            }

            public virtual void AssertIsValid()
            {
                if (Construct == null)
                    throw new InvalidOperationException("Operation {0} does not hace Construct initialized".FormatWith(operationSymbol));
            }

            public override string ToString()
            {
                return "{0} ConstructFrom {1} -> {2}".FormatWith(operationSymbol, typeof(F), typeof(T));
            }

        }

        public class ConstructFromMany<F> : IConstructorFromManyOperation
            where F : class, IEntity
        {
            protected readonly OperationSymbol operationSymbol;
            OperationSymbol IOperation.OperationSymbol { get { return operationSymbol; } }
            Type IOperation.OverridenType { get { return typeof(F); } }
            OperationType IOperation.OperationType { get { return OperationType.ConstructorFromMany; } }
            bool IOperation.Returns { get { return true; } }
            Type IOperation.ReturnType { get { return typeof(T); } }

            protected readonly Type baseType;
            Type IConstructorFromManyOperation.BaseType { get { return baseType; } }
            IEnumerable<Enum> IOperation.UntypedFromStates { get { return null; } }
            IEnumerable<Enum> IOperation.UntypedToStates { get { return Enumerable.Empty<Enum>(); } }
            Type IOperation.StateType { get { return null; } }

            public bool LogAlsoIfNotSaved { get; set; }

            public Func<List<Lite<F>>, object[], T> Construct { get; set; }

            public void OverrideConstruct(Overrider<Func<List<Lite<F>>, object[], T>> overrider)
            {
                this.Construct = overrider(this.Construct);
            }

            public ConstructFromMany(ConstructSymbol<T>.FromMany<F> symbol)
            {
                if (symbol == null)
                    throw AutoInitAttribute.ArgumentNullException(typeof(ConstructSymbol<T>.FromMany<F>), nameof(symbol));

                this.operationSymbol = symbol.Symbol;
                this.baseType = symbol.BaseType;

            }

            protected ConstructFromMany(OperationSymbol operationSymbol, Type baseType)
            {
                this.operationSymbol = operationSymbol ?? throw new ArgumentNullException(nameof(operationSymbol));
                this.baseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
            }

            public static ConstructFromMany<F> Untyped<B>(ConstructSymbol<B>.FromMany<F> symbol)
                where B : class, IEntity
            {
                return new ConstructFromMany<F>(symbol.Symbol, symbol.BaseType);
            }


            IEntity IConstructorFromManyOperation.Construct(IEnumerable<Lite<IEntity>> lites, params object[] args)
            {
                using (HeavyProfiler.Log("ConstructFromMany", () => operationSymbol.Key))
                {
                    foreach (var type in lites.Select(a => a.EntityType).Distinct())
                    {
                        OperationLogic.AssertOperationAllowed(operationSymbol, type, inUserInterface: false);
                    }

                    OperationLogEntity log = new OperationLogEntity
                    {
                        Operation = operationSymbol,
                        Start = TimeZoneManager.Now,
                        User = UserHolder.Current?.ToLite()
                    };

                    try
                    {
                        using (Transaction tr = new Transaction())
                        {
                            T result = null;

                            using (OperationLogic.AllowSave<F>())
                            using (OperationLogic.AllowSave<T>())
                                OperationLogic.OnSuroundOperation(this, log, null, args).EndUsing(_ =>
                                {
                                    result = OnConstruct(lites.Cast<Lite<F>>().ToList(), args);

                                    AssertEntity(result);

                                    if ((result != null && !result.IsNew) || LogAlsoIfNotSaved)
                                    {
                                        log.End = TimeZoneManager.Now;
                                        log.SetTarget(result);
                                    }
                                    else
                                    {
                                        log = null;
                                    }
                                });

                            if (log != null)
                                log.SaveLog();

                            return tr.Commit(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        OperationLogic.SetExceptionData(ex, operationSymbol, null, args);

                        if (LogAlsoIfNotSaved)
                        {
                            if (Transaction.InTestTransaction)
                                throw;

                            var exLog = ex.LogException();

                            using (Transaction tr2 = Transaction.ForceNew())
                            {
                                log.Exception = exLog.ToLite();

                                log.SaveLog();

                                tr2.Commit();
                            }
                        }

                        throw;
                    }
                }
            }

            protected virtual T OnConstruct(List<Lite<F>> lites, object[] args)
            {
                return Construct(lites, args);
            }

            protected virtual void AssertEntity(T entity)
            {
            }

            public virtual void AssertIsValid()
            {
                if (Construct == null)
                    throw new InvalidOperationException("Operation {0} Constructor initialized".FormatWith(operationSymbol));
            }

            public override string ToString()
            {
                return "{0} ConstructFromMany {1} -> {2}".FormatWith(operationSymbol, typeof(F), typeof(T));
            }
        }

        public class Execute : _Execute<T>, IExecuteOperation
        {
            protected readonly ExecuteSymbol<T> Symbol;
            OperationSymbol IOperation.OperationSymbol { get { return Symbol.Symbol; } }
            Type IOperation.OverridenType { get { return typeof(T); } }
            OperationType IOperation.OperationType { get { return OperationType.Execute; } }
            public bool Lite { get; set; }
            bool IOperation.Returns { get { return true; } }
            Type IOperation.ReturnType { get { return null; } }
            Type IOperation.StateType { get { return null; } }

            Type IEntityOperation.BaseType { get { return Symbol.BaseType; } }
            bool IEntityOperation.HasCanExecute { get { return CanExecute != null; } }
            IEnumerable<Enum> IOperation.UntypedFromStates { get { return Enumerable.Empty<Enum>(); } }
            IEnumerable<Enum> IOperation.UntypedToStates { get { return Enumerable.Empty<Enum>(); } }

            public bool AllowsNew { get; set; }

            //public Action<T, object[]> Execute { get; set; } (inherited)
            public Func<T, string> CanExecute { get; set; }

            public Execute OverrideCanExecute(Overrider<Func<T, string>> overrider)
            {
                this.CanExecute = overrider(this.CanExecute ?? (t => null));
                return this;
            }

            public void OverrideExecute(Overrider<Action<T, object[]>> overrider)
            {
                this.Execute = overrider(this.Execute);
            }

            public Execute(ExecuteSymbol<T> symbol)
            {
                this.Symbol = symbol ?? throw AutoInitAttribute.ArgumentNullException(typeof(ExecuteSymbol<T>), nameof(symbol));
                this.Lite = true;
            }

            string IEntityOperation.CanExecute(IEntity entity)
            {
                return OnCanExecute((T)entity);
            }

            protected virtual string OnCanExecute(T entity)
            {
                if (entity.IsNew && !AllowsNew)
                    return EngineMessage.TheEntity0IsNew.NiceToString().FormatWith(entity);

                if (CanExecute != null)
                    return CanExecute(entity);

                return null;
            }

            void IExecuteOperation.Execute(IEntity entity, params object[] args)
            {
                using (HeavyProfiler.Log("Execute", () => Symbol.Symbol.Key))
                {
                    OperationLogic.AssertOperationAllowed(Symbol.Symbol, entity.GetType(), inUserInterface: false);

                    string error = OnCanExecute((T)entity);
                    if (error != null)
                        throw new ApplicationException(error);

                    OperationLogEntity log = new OperationLogEntity
                    {
                        Operation = Symbol.Symbol,
                        Start = TimeZoneManager.Now,
                        User = UserHolder.Current?.ToLite()
                    };

                    try
                    {
                        using (Transaction tr = new Transaction())
                        {
                            using (OperationLogic.AllowSave(entity.GetType()))
                                OperationLogic.OnSuroundOperation(this, log, entity, args).EndUsing(_ =>
                                {
                                    Execute((T)entity, args);

                                    AssertEntity((T)entity);

                                    entity.Save(); //Nothing happens if already saved

                                    log.SetTarget(entity);
                                    log.End = TimeZoneManager.Now;
                                });

                            log.SaveLog();

                            tr.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        OperationLogic.SetExceptionData(ex, Symbol.Symbol, (Entity)entity, args);

                        if (Transaction.InTestTransaction)
                            throw;

                        var exLog = ex.LogException();

                        using (Transaction tr2 = Transaction.ForceNew())
                        {
                            OperationLogEntity newLog = new OperationLogEntity //Transaction chould have been rollbacked just before commiting
                            {
                                Operation = log.Operation,
                                Start = log.Start,
                                User = log.User,
                                Target = entity.IsNew ? null : entity.ToLite(),
                                Exception = exLog.ToLite(),
                            };

                            newLog.SaveLog();

                            tr2.Commit();
                        }

                        throw;
                    }
                }
            }

            protected virtual void AssertEntity(T entity)
            {
            }

            public virtual void AssertIsValid()
            {
                if (Execute == null)
                    throw new InvalidOperationException("Operation {0} does not have Execute initialized".FormatWith(Symbol));
            }

            public override string ToString()
            {
                return "{0} Execute on {1}".FormatWith(Symbol, typeof(T));
            }
        }

        public class Delete : _Delete<T>, IDeleteOperation
        {
            protected readonly DeleteSymbol<T> Symbol;
            OperationSymbol IOperation.OperationSymbol { get { return Symbol.Symbol; } }
            Type IOperation.OverridenType { get { return typeof(T); } }
            OperationType IOperation.OperationType { get { return OperationType.Delete; } }
            public bool Lite { get; set; }
            bool IOperation.Returns { get { return false; } }
            Type IOperation.ReturnType { get { return null; } }
            IEnumerable<Enum> IOperation.UntypedFromStates { get { return Enumerable.Empty<Enum>(); } }
            IEnumerable<Enum> IOperation.UntypedToStates { get { return null; } }
            Type IOperation.StateType { get { return null; } }

            public bool AllowsNew { get { return false; } }

            Type IEntityOperation.BaseType { get { return Symbol.BaseType; } }
            bool IEntityOperation.HasCanExecute { get { return CanDelete != null; } }

            //public Action<T, object[]> Delete { get; set; } (inherited)
            public Func<T, string> CanDelete { get; set; }

            public Delete OverrideCanDelete(Overrider<Func<T, string>> overrider)
            {
                this.CanDelete = overrider(this.CanDelete ?? (t => null));
                return this;
            }

            public void OverrideDelete(Overrider<Action<T, object[]>> overrider)
            {
                this.Delete = overrider(this.Delete);
            }

            public Delete(DeleteSymbol<T> symbol)
            {
                this.Symbol = symbol ?? throw AutoInitAttribute.ArgumentNullException(typeof(DeleteSymbol<T>), nameof(symbol));
                this.Lite = true;
            }

            string IEntityOperation.CanExecute(IEntity entity)
            {
                return OnCanDelete((T)entity);
            }

            protected virtual string OnCanDelete(T entity)
            {
                if (entity.IsNew)
                    return EngineMessage.TheEntity0IsNew.NiceToString().FormatWith(entity);

                if (CanDelete != null)
                    return CanDelete(entity);

                return null;
            }

            void IDeleteOperation.Delete(IEntity entity, params object[] args)
            {
                using (HeavyProfiler.Log("Delete", () => Symbol.Symbol.Key))
                {
                    OperationLogic.AssertOperationAllowed(Symbol.Symbol, entity.GetType(), inUserInterface: false);

                    string error = OnCanDelete((T)entity);
                    if (error != null)
                        throw new ApplicationException(error);

                    OperationLogEntity log = new OperationLogEntity
                    {
                        Operation = Symbol.Symbol,
                        Start = TimeZoneManager.Now,
                        User = UserHolder.Current?.ToLite()
                    };

                    using (OperationLogic.AllowSave(entity.GetType()))
                        OperationLogic.OnSuroundOperation(this, log, entity, args).EndUsing(_ =>
                        {
                            try
                            {
                                using (Transaction tr = new Transaction())
                                {
                                    OnDelete((T)entity, args);

                                    log.SetTarget(entity);
                                    log.End = TimeZoneManager.Now;

                                    log.SaveLog();

                                    tr.Commit();
                                }
                            }
                            catch (Exception ex)
                            {
                                OperationLogic.SetExceptionData(ex, Symbol.Symbol, (Entity)entity, args);

                                if (Transaction.InTestTransaction)
                                    throw;

                                var exLog = ex.LogException();

                                using (Transaction tr2 = Transaction.ForceNew())
                                {
                                    log.Target = entity.ToLite();
                                    log.Exception = exLog.ToLite();

                                    log.SaveLog();

                                    tr2.Commit();
                                }

                                throw;
                            }
                        });
                }
            }

            protected virtual void OnDelete(T entity, object[] args)
            {
                Delete(entity, args);
            }

            public virtual void AssertIsValid()
            {
                if (Delete == null)
                    throw new InvalidOperationException("Operation {0} does not have Delete initialized".FormatWith(Symbol.Symbol));
            }

            public override string ToString()
            {
                return "{0} Delete {1}".FormatWith(Symbol.Symbol, typeof(T));
            }
        }
    }
}
