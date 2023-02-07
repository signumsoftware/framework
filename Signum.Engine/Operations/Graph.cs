using Signum.Engine.Basics;
using Signum.Engine.Operations.Internal;
using Signum.Entities;
using Signum.Entities.Basics;
using System.Collections;

namespace Signum.Engine.Operations;

public delegate F Overrider<F>(F baseFunc);

public class Graph<T>
     where T : class, IEntity
{
    public class Construct : _Construct<T>, IConstructOperation
    {
        protected readonly OperationSymbol operationSymbol;
        OperationSymbol IOperation.OperationSymbol => operationSymbol;
        Type IOperation.OverridenType => typeof(T);
        OperationType IOperation.OperationType => OperationType.Constructor;
        bool IOperation.Returns => true;
        Type? IOperation.ReturnType => typeof(T);
        IList? IOperation.UntypedFromStates => null;
        IList? IOperation.UntypedToStates => new List<Enum>();
        Type? IOperation.StateType => null;
        LambdaExpression? IOperation.GetStateExpression() => null;

        public bool LogAlsoIfNotSaved { get; set; }

        //public Func<object[]?, T> Construct { get; set; } (inherited)
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

        public void OverrideConstruct(Overrider<Func<object?[]?, T?>> overrider)
        {
            this.Construct = overrider(this.Construct);
        }

        IEntity IConstructOperation.Construct(params object?[]? args)
        {
            using (HeavyProfiler.Log("Construct", () => operationSymbol.Key))
            {
                OperationLogic.AssertOperationAllowed(operationSymbol, typeof(T), inUserInterface: false);

                OperationLogEntity? log = new OperationLogEntity
                {
                    Operation = operationSymbol,
                    Start = Clock.Now,
                    User = UserHolder.Current?.User!,
                };

                try
                {
                    using (var tr = new Transaction())
                    {
                        T? result = null;
                        using (OperationLogic.AllowSave<T>())
                            OperationLogic.OnSuroundOperation(this, log, null, args).EndUsing(_ =>
                            {
                                result = Construct(args);

                                if (result != null)
                                    AssertEntity(result);

                                if ((result != null && !result.IsNew) || LogAlsoIfNotSaved)
                                {
                                    log.SetTarget(result);
                                    log.End = Clock.Now;
                                }
                                else
                                    log = null;
                            });

                        if (log != null)
                            log.SaveLog();

                        return tr.Commit(result!);
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

                        if (log != null)
                        {
                            using (var tr2 = Transaction.ForceNew())
                            {
                                log.Exception = exLog.ToLite();
                                log.End = Clock.Now;
                                log.SaveLog();

                                tr2.Commit();
                            }
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
        OperationSymbol IOperation.OperationSymbol => operationSymbol;
        Type IOperation.OverridenType => typeof(F);
        OperationType IOperation.OperationType => OperationType.ConstructorFrom;
        IList? IOperation.UntypedFromStates => null;
        IList? IOperation.UntypedToStates => new List<Enum>();
        Type? IOperation.StateType => null;
        LambdaExpression? IOperation.GetStateExpression() => null;

        public bool CanBeModified { get; set; }
        public bool LogAlsoIfNotSaved { get; set; }

        bool IOperation.Returns { get { return true; } }
        Type? IOperation.ReturnType { get { return typeof(T); } }

        protected readonly Type baseType;
        Type IEntityOperation.BaseType { get { return baseType; } }
        bool IEntityOperation.HasCanExecute { get { return CanConstruct != null; } }

        public bool CanBeNew { get; set; }

        public Func<F, string?>? CanConstruct { get; set; }

        public Expression<Func<F, string?>>? CanConstructExpression { get; set; }

        public ConstructFrom<F> OverrideCanConstruct(Overrider<Func<F, string?>> overrider)
        {
            this.CanConstruct = overrider(this.CanConstruct ?? (f => null));
            return this;
        }

        public Func<F, object?[]?, T?> Construct { get; set; } = null!;

        public void OverrideConstruct(Overrider<Func<F, object?[]?, T?>> overrider)
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

        LambdaExpression? IEntityOperation.CanExecuteExpression()
        {
            return CanConstructExpression;
        }

        string? IEntityOperation.CanExecute(IEntity entity)
        {
            return OnCanConstruct(entity);
        }

        string? OnCanConstruct(IEntity entity)
        {
            if (entity.IsNew && !CanBeNew)
                return EngineMessage.TheEntity0IsNew.NiceToString().FormatWith(entity);

            if (CanConstruct != null)
                return CanConstruct((F)entity);

            return null;
        }

        IEntity IConstructorFromOperation.Construct(IEntity origin, params object?[]? args)
        {
            using (HeavyProfiler.Log("ConstructFrom", () => operationSymbol.Key))
            {
                OperationLogic.AssertOperationAllowed(operationSymbol, origin.GetType(), inUserInterface: false);

                string? error = OnCanConstruct(origin);
                if (error != null)
                    throw new ApplicationException(error);

                OperationLogEntity? log = new OperationLogEntity
                {
                    Operation = operationSymbol,
                    Start = Clock.Now,
                    User = UserHolder.Current?.User!,
                    Origin = origin.ToLite(origin.IsNew),
                };

                try
                {
                    using (var tr = new Transaction())
                    {
                        T? result = null;
                        using (OperationLogic.AllowSave(origin.GetType()))
                        using (OperationLogic.AllowSave<T>())
                            OperationLogic.OnSuroundOperation(this, log, origin, args).EndUsing(_ =>
                            {
                                result = Construct((F)origin, args);

                                if (result != null)
                                    AssertEntity(result);

                                if ((result != null && !result.IsNew) || LogAlsoIfNotSaved)
                                {
                                    log.End = Clock.Now;
                                    log.SetTarget(result);
                                }
                                else
                                {
                                    log = null;
                                }
                            });

                        if (log != null)
                            log.SaveLog();

                        return tr.Commit(result!);
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

                        if (log != null)
                        {
                            using (var tr2 = Transaction.ForceNew())
                            {
                                log.Exception = exLog.ToLite();
                                log.End = Clock.Now;
                                log.SaveLog();

                                tr2.Commit();
                            }
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
            if (CanConstruct == null && CanConstructExpression != null)
                CanConstruct = CanConstructExpression.Compile();

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
        OperationSymbol IOperation.OperationSymbol => operationSymbol;
        Type IOperation.OverridenType => typeof(F);
        OperationType IOperation.OperationType => OperationType.ConstructorFromMany;
        bool IOperation.Returns => true;
        Type? IOperation.ReturnType => typeof(T);

        protected readonly Type baseType;
        Type IConstructorFromManyOperation.BaseType => baseType;
        IList? IOperation.UntypedFromStates => null;
        IList? IOperation.UntypedToStates => new List<Enum>();
        Type? IOperation.StateType => null;
        LambdaExpression? IOperation.GetStateExpression() => null;

        public bool LogAlsoIfNotSaved { get; set; }

        public Func<List<Lite<F>>, object?[]?, T?> Construct { get; set; } = null!;

        public void OverrideConstruct(Overrider<Func<List<Lite<F>>, object?[]?, T?>> overrider)
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


        IEntity IConstructorFromManyOperation.Construct(IEnumerable<Lite<IEntity>> lites, params object?[]? args)
        {
            using (HeavyProfiler.Log("ConstructFromMany", () => operationSymbol.Key))
            {
                foreach (var type in lites.Select(a => a.EntityType).Distinct())
                {
                    OperationLogic.AssertOperationAllowed(operationSymbol, type, inUserInterface: false);
                }

                OperationLogEntity? log = new OperationLogEntity
                {
                    Operation = operationSymbol,
                    Start = Clock.Now,
                    User = UserHolder.Current?.User!
                };

                try
                {
                    using (var tr = new Transaction())
                    {
                        T? result = null;

                        using (OperationLogic.AllowSave<F>())
                        using (OperationLogic.AllowSave<T>())
                            OperationLogic.OnSuroundOperation(this, log, null, args).EndUsing(_ =>
                            {
                                result = OnConstruct(lites.Cast<Lite<F>>().ToList(), args);

                                if (result != null)
                                    AssertEntity(result);

                                if ((result != null && !result.IsNew) || LogAlsoIfNotSaved)
                                {
                                    log.End = Clock.Now;
                                    log.SetTarget(result);
                                }
                                else
                                {
                                    log = null;
                                }
                            });

                        if (log != null)
                            log.SaveLog();

                        return tr.Commit(result!);
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
                        if (log != null)
                        {
                            using (var tr2 = Transaction.ForceNew())
                            {
                                log.Exception = exLog.ToLite();
                                log.End = Clock.Now;
                                log.SaveLog();

                                tr2.Commit();
                            }
                        }
                    }

                    throw;
                }
            }
        }

        protected virtual T? OnConstruct(List<Lite<F>> lites, object?[]? args)
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
        OperationSymbol IOperation.OperationSymbol => Symbol.Symbol;
        Type IOperation.OverridenType => typeof(T);
        OperationType IOperation.OperationType => OperationType.Execute;
        public bool CanBeModified { get; set; }
        bool IOperation.Returns => true;
        Type? IOperation.ReturnType => null;
        Type? IOperation.StateType => null;
        LambdaExpression? IOperation.GetStateExpression() => null;
        public bool AvoidImplicitSave { get; set; }

        Type IEntityOperation.BaseType => Symbol.BaseType;
        bool IEntityOperation.HasCanExecute => CanExecute != null;
        IList? IOperation.UntypedFromStates => new List<Enum>();
        IList? IOperation.UntypedToStates => new List<Enum>();

        public bool CanBeNew { get; set; }

        //public Action<T, object[]?> Execute { get; set; } (inherited)
        public Func<T, string?>? CanExecute { get; set; }

        public Expression<Func<T, string?>>? CanExecuteExpression { get; set; }

        public Execute OverrideCanExecute(Overrider<Func<T, string?>> overrider)
        {
            this.CanExecute = overrider(this.CanExecute ?? (t => null));
            return this;
        }

        public void OverrideExecute(Overrider<Action<T, object?[]?>> overrider)
        {
            this.Execute = overrider(this.Execute);
        }

        public Execute(ExecuteSymbol<T> symbol)
        {
            this.Symbol = symbol ?? throw AutoInitAttribute.ArgumentNullException(typeof(ExecuteSymbol<T>), nameof(symbol));
        }

        LambdaExpression? IEntityOperation.CanExecuteExpression()
        {
            return CanExecuteExpression;
        }

        string? IEntityOperation.CanExecute(IEntity entity)
        {
            return OnCanExecute((T)entity);
        }

        protected virtual string? OnCanExecute(T entity)
        {
            if (entity.IsNew && !CanBeNew)
                return EngineMessage.TheEntity0IsNew.NiceToString().FormatWith(entity);

            if (CanExecute != null)
                return CanExecute(entity);

            return null;
        }

        void IExecuteOperation.Execute(IEntity entity, params object?[]? args)
        {
            using (HeavyProfiler.Log("Execute", () => Symbol.Symbol.Key))
            {
                OperationLogic.AssertOperationAllowed(Symbol.Symbol, entity.GetType(), inUserInterface: false);

                string? error = OnCanExecute((T)entity);
                if (error != null)
                {
                    var ex= new ApplicationException(error);
                    OperationLogic.OnOperationExceptionHandlerArgs(Symbol.Symbol, entity,ex, args);
                    throw ex;

                }

                OperationLogEntity log = new OperationLogEntity
                {
                    Operation = Symbol.Symbol,
                    Start = Clock.Now,
                    User = UserHolder.Current?.User!
                };

                try
                {

                    OperationLogic.OnOperationBeforeExecuteHandlerArgs(Symbol.Symbol, entity, args);


                    using (var tr = new Transaction())
                    {
                        using (OperationLogic.AllowSave(entity.GetType()))
                        {
                            var assertEnd = AssertEntity((T)entity);
                            OperationLogic.OnSuroundOperation(this, log, entity, args).EndUsing(_ =>
                            {
                                Execute((T)entity, args);

                                assertEnd?.Invoke();

                                if (!AvoidImplicitSave)
                                    entity.Save(); //Nothing happens if already saved

                                log.SetTarget(entity);
                                log.End = Clock.Now;
                            });
                        }

                        log.SaveLog();

                        tr.Commit();
                    }

                    OperationLogic.OnOperationExecutedHandlerArgs(Symbol.Symbol, entity, args);
                }
                catch (Exception ex)
                {
                    OperationLogic.SetExceptionData(ex, Symbol.Symbol, (Entity)entity, args);

                    if (Transaction.InTestTransaction)
                    {
                        OperationLogic.OnOperationExceptionHandlerArgs(Symbol.Symbol, entity, ex, args);
                        throw;
                    }
                        

                    var exLog = ex.LogException();

                    using (var tr2 = Transaction.ForceNew())
                    {
                        OperationLogEntity newLog = new OperationLogEntity //Transaction chould have been rollbacked just before commiting
                        {
                            Operation = log.Operation,
                            Start = log.Start,
                            User = log.User,
                            End = Clock.Now,
                            Target = entity.IsNew ? null : entity.ToLite(),
                            Exception = exLog.ToLite(),
                        };

                        newLog.SaveLog();

                        tr2.Commit();
                    }

                    OperationLogic.OnOperationExceptionHandlerArgs(Symbol.Symbol, entity, ex, args);

                    throw;
                }
            }
        }

        protected virtual Action? AssertEntity(T entity)
        {
            return null;
        }

        public virtual void AssertIsValid()
        {
            if (CanExecute == null && CanExecuteExpression != null)
                CanExecute = CanExecuteExpression.Compile();

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
        OperationSymbol IOperation.OperationSymbol => Symbol.Symbol;
        Type IOperation.OverridenType => typeof(T);
        OperationType IOperation.OperationType => OperationType.Delete;
        public bool CanBeModified { get; set; }
        bool IOperation.Returns => false;
        Type? IOperation.ReturnType => null;
        IList? IOperation.UntypedFromStates => new List<Enum>();
        IList? IOperation.UntypedToStates => null;
        Type? IOperation.StateType => null;
        LambdaExpression? IOperation.GetStateExpression() => null;

        public bool CanBeNew { get { return false; } }

        Type IEntityOperation.BaseType { get { return Symbol.BaseType; } }
        bool IEntityOperation.HasCanExecute { get { return CanDelete != null; } }

        //public Action<T, object[]?> Delete { get; set; } (inherited)
        public Func<T, string?>? CanDelete { get; set; }

        public Expression<Func<T, string?>>? CanDeleteExpression { get; set; }

        public Delete OverrideCanDelete(Overrider<Func<T, string?>> overrider)
        {
            this.CanDelete = overrider(this.CanDelete ?? (t => null));
            return this;
        }

        public void OverrideDelete(Overrider<Action<T, object?[]?>> overrider)
        {
            this.Delete = overrider(this.Delete);
        }

        public Delete(DeleteSymbol<T> symbol)
        {
            this.Symbol = symbol ?? throw AutoInitAttribute.ArgumentNullException(typeof(DeleteSymbol<T>), nameof(symbol));
        }

        LambdaExpression? IEntityOperation.CanExecuteExpression()
        {
            return CanDeleteExpression;
        }

        string? IEntityOperation.CanExecute(IEntity entity)
        {
            return OnCanDelete((T)entity);
        }

        protected virtual string? OnCanDelete(T entity)
        {
            if (entity.IsNew)
                return EngineMessage.TheEntity0IsNew.NiceToString().FormatWith(entity);

            if (CanDelete != null)
                return CanDelete(entity);

            return null;
        }

        void IDeleteOperation.Delete(IEntity entity, params object?[]? args)
        {
            using (HeavyProfiler.Log("Delete", () => Symbol.Symbol.Key))
            {
                OperationLogic.AssertOperationAllowed(Symbol.Symbol, entity.GetType(), inUserInterface: false);

                string? error = OnCanDelete((T)entity);
                if (error != null)
                    throw new ApplicationException(error);

                OperationLogEntity log = new OperationLogEntity
                {
                    Operation = Symbol.Symbol,
                    Start = Clock.Now,
                    User = UserHolder.Current?.User!,
                };

                using (OperationLogic.AllowSave(entity.GetType()))
                    OperationLogic.OnSuroundOperation(this, log, entity, args).EndUsing(_ =>
                    {
                        try
                        {

                            OperationLogic.OnOperationBeforeExecuteHandlerArgs(Symbol.Symbol, entity, args);


                            using (var tr = new Transaction())
                            {
                                OnDelete((T)entity, args);

                                log.SetTarget(entity);
                                log.End = Clock.Now;

                                log.SaveLog();

                                tr.Commit();
                            }

                            OperationLogic.OnOperationExecutedHandlerArgs(Symbol.Symbol, entity, args);
                        }
                        catch (Exception ex)
                        {
                            OperationLogic.SetExceptionData(ex, Symbol.Symbol, (Entity)entity, args);

                            if (Transaction.InTestTransaction)
                                throw;

                            var exLog = ex.LogException();

                            using (var tr2 = Transaction.ForceNew())
                            {
                                log.Target = entity.ToLite();
                                log.Exception = exLog.ToLite();
                                log.End = Clock.Now;
                                log.SaveLog();

                                tr2.Commit();
                            }

                            throw;
                        }
                    });
            }
        }

        protected virtual void OnDelete(T entity, object?[]? args)
        {
            Delete(entity, args);
        }

        public virtual void AssertIsValid()
        {
            if (CanDelete == null && CanDeleteExpression != null)
                CanDelete = CanDeleteExpression.Compile();

            if (Delete == null)
                throw new InvalidOperationException("Operation {0} does not have Delete initialized".FormatWith(Symbol.Symbol));
        }

        public override string ToString()
        {
            return "{0} Delete {1}".FormatWith(Symbol.Symbol, typeof(T));
        }
    }
}
