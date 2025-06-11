using System.Collections;

namespace Signum.Operations;

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
        Type? IOperation.ReturnType => typeof(T);
        IList? IOperation.UntypedFromStates => null;
        IList? IOperation.UntypedToStates => new List<Enum>();
        Type? IOperation.StateType => null;
        LambdaExpression? IOperation.GetStateExpression() => null;

        public bool LogAlsoIfNotSaved { get; set; }

        //public Func<object[]?, T> Construct { get; set; } (inherited)
        public bool Lite { get { return false; } }

        public Construct(ConstructSymbol<T>.Simple symbol) : this(symbol.Symbol)
        {
           
        }

        Construct(OperationSymbol symbol)
        {
            if (symbol == null)
                throw AutoInitAttribute.ArgumentNullException(typeof(ConstructSymbol<T>.Simple), nameof(symbol));

            this.operationSymbol = symbol;
        }

        public static Construct Untyped(IOperationSymbolContainer symbol)
        {
            return new Construct(symbol.Symbol);
        }

        public void OverrideConstruct(Overrider<Func<object?[]?, T?>> overrider)
        {
            Construct = overrider(Construct);
        }

        IEntity IConstructOperation.Construct(params object?[]? args)
        {
            var currentUser = UserHolder.Current?.User!;
            using (var trace = HeavyProfiler.Log("Construct", new StructuredLogMessage("{Operation} done by {User}", operationSymbol.Key, currentUser)))
            {
                if (trace?.activity is { } ac)
                {
                    ac.SetTag("Operation", operationSymbol.Key);
                    ac.SetTag("User", currentUser);
                }

                OperationLogic.AssertOperationAllowed(operationSymbol, typeof(T), inUserInterface: false, entity: null);

                OperationLogEntity? log = new OperationLogEntity
                {
                    Operation = operationSymbol,
                    Start = Clock.Now,
                    User = currentUser,
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

                                if (result != null && !result.IsNew || LogAlsoIfNotSaved)
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
                                OperationLogEntity newLog = new OperationLogEntity //Transaction has been rollbacked
                                {
                                    Operation = log.Operation,
                                    Start = log.Start,
                                    User = log.User,
                                    End = Clock.Now,
                                    Target = null,
                                    Exception = exLog.ToLite(),
                                };

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

        public LambdaExpression? CanExecuteExpression() => null;

      
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

        Type? IOperation.ReturnType { get { return typeof(T); } }

        protected readonly Type baseType;
        Type IEntityOperation.BaseType { get { return baseType; } }
        bool IEntityOperation.HasCanExecute { get { return CanConstruct != null; } }

        public bool CanBeNew { get; set; }
        public bool ResultIsSaved { get; set; }


        public Func<F, string?>? CanConstruct { get; set; }

        public Expression<Func<F, string?>>? CanConstructExpression { get; set; }

        public ConstructFrom<F> OverrideCanConstruct(Overrider<Func<F, string?>> overrider)
        {
            CanConstruct = overrider(CanConstruct ?? (f => null));
            return this;
        }

        public Func<F, object?[]?, T?> Construct { get; set; } = null!;

        public void OverrideConstruct(Overrider<Func<F, object?[]?, T?>> overrider)
        {
            Construct = overrider(Construct);
        }

        ConstructFrom(OperationSymbol symbol, Type baseType)
        {
            if (symbol == null)
                throw AutoInitAttribute.ArgumentNullException(typeof(ConstructSymbol<T>.From<F>), nameof(symbol));

            this.operationSymbol = symbol;
            this.baseType = baseType;
        }

        public ConstructFrom(ConstructSymbol<T>.From<F> symbol): this(symbol.Symbol, symbol.BaseType)
        {
        }

        public static ConstructFrom<F> Untyped(IEntityOperationSymbolContainer<F> symbol)
        {
            return new ConstructFrom<F>(symbol.Symbol, symbol.BaseType);
        }

        LambdaExpression? IOperation.CanExecuteExpression()
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
            var currentUser = UserHolder.Current?.User!;
            using (var trace = HeavyProfiler.Log("ConstructFrom", new StructuredLogMessage("{Operation} on {Entity} done by {User}", operationSymbol.Key, origin, currentUser)))
            {
                if (trace?.activity is { } ac)
                {
                    ac.SetTag("Operation", operationSymbol.Key);
                    ac.SetTag("Origin", origin);
                    ac.SetTag("User", currentUser);
                }

                OperationLogic.AssertOperationAllowed(operationSymbol, origin.GetType(), inUserInterface: false, entity: (Entity)origin);

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
                                string? error = OnCanConstruct(origin);
                                if (error != null)
                                    throw new ApplicationException(error);

                                result = Construct((F)origin, args);

                                if (result != null)
                                    AssertEntity(result);

                                if (result != null && !result.IsNew || LogAlsoIfNotSaved)
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
                                OperationLogEntity newLog = new OperationLogEntity //Transaction has been rollbacked
                                {
                                    Operation = log.Operation,
                                    Start = log.Start,
                                    User = log.User,
                                    End = Clock.Now,
                                    Origin = log.Origin,
                                    Target = null,
                                    Exception = exLog.ToLite(),
                                };
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
            if (ResultIsSaved && entity.IsNew)
                throw new InvalidOperationException("After executing {0} the entity should be saved".FormatWith(operationSymbol));
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
        protected readonly ConstructSymbol<T>.FromMany<F> constructSymbol;
        protected readonly OperationSymbol operationSymbol;
        OperationSymbol IOperation.OperationSymbol => operationSymbol;
        Type IOperation.OverridenType => typeof(F);
        OperationType IOperation.OperationType => OperationType.ConstructorFromMany;
        Type? IOperation.ReturnType => typeof(T);

        protected readonly Type baseType;
        Type IConstructorFromManyOperation.BaseType => baseType;
        IList? IOperation.UntypedFromStates => null;
        IList? IOperation.UntypedToStates => new List<Enum>();
        Type? IOperation.StateType => null;
        LambdaExpression? IOperation.GetStateExpression() => null;

        public bool LogAlsoIfNotSaved { get; set; }

        public Expression<Func<F, string?>>? CanConstructExpression { get; set; }

        LambdaExpression? IOperation.CanExecuteExpression()
        {
            return CanConstructExpression;
        }

        public Func<List<Lite<F>>, object?[]?, T?> Construct { get; set; } = null!;

        public void OverrideConstruct(Overrider<Func<List<Lite<F>>, object?[]?, T?>> overrider)
        {
            Construct = overrider(Construct);
        }

        public ConstructFromMany(ConstructSymbol<T>.FromMany<F> symbol)
        {
            if (symbol == null)
                throw AutoInitAttribute.ArgumentNullException(typeof(ConstructSymbol<T>.FromMany<F>), nameof(symbol));

            this.constructSymbol = symbol;
            this.operationSymbol = symbol.Symbol;
            baseType = symbol.BaseType;
        }

        string? OnCanConstruct(IEnumerable<Lite<IEntity>> lites)
        {
            if (CanConstructExpression != null)
            {
                var errors = lites.GroupBy(a => a.EntityType)
                    .SelectMany(gr => gr.Chunk(100).SelectMany(ch => OperationLogic.giGetCanExecute.GetInvoker(gr.Key)(this, ch).Values))
                    .Distinct().ToList();

                if (errors.Any())
                    return errors.ToString("\n");

                return null;
            }

            return null;
        }

        IEntity IConstructorFromManyOperation.Construct(IEnumerable<Lite<IEntity>> lites, params object?[]? args)
        {
            var currentUser = UserHolder.Current?.User!;
            using (var trace = HeavyProfiler.Log("ConstructFromMany", new StructuredLogMessage("{Operation} done by {User}", this.operationSymbol.Key, currentUser)))
            {
                if (trace?.activity is { } ac)
                {
                    ac.SetTag("Operation", this.operationSymbol.Key);
                    ac.SetTag("User", currentUser);
                }

                foreach (var type in lites.Select(a => a.EntityType).Distinct())
                {
                    OperationLogic.AssertOperationAllowed(this.operationSymbol, type, inUserInterface: false, entity: null);
                }

                OperationLogEntity? log = new OperationLogEntity
                {
                    Operation = this.operationSymbol,
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
                                string? error = OnCanConstruct(lites);
                                if (error != null)
                                    throw new ApplicationException(error);

                                result = OnConstruct(lites.Cast<Lite<F>>().ToList(), args);

                                if (result != null)
                                    AssertEntity(result);

                                if (result != null && !result.IsNew || LogAlsoIfNotSaved)
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
                                OperationLogEntity newLog = new OperationLogEntity //Transaction has been rollbacked
                                {
                                    Operation = log.Operation,
                                    Start = log.Start,
                                    User = log.User,
                                    End = Clock.Now,
                                    Target = null,
                                    Exception = exLog.ToLite(),
                                };
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
        protected readonly ExecuteSymbol<T> executeSymbol;
        protected readonly OperationSymbol operationSymbol;
        
        OperationSymbol IOperation.OperationSymbol => executeSymbol.Symbol;
        Type IOperation.OverridenType => typeof(T);
        OperationType IOperation.OperationType => OperationType.Execute;
        public bool CanBeModified { get; set; }
        Type? IOperation.ReturnType => null;
        Type? IOperation.StateType => null;
        LambdaExpression? IOperation.GetStateExpression() => null;
        public bool AvoidImplicitSave { get; set; }
        public bool ForReadonlyEntity { get; set; }

        Type IEntityOperation.BaseType => executeSymbol.BaseType;
        bool IEntityOperation.HasCanExecute => CanExecute != null;
        IList? IOperation.UntypedFromStates => new List<Enum>();
        IList? IOperation.UntypedToStates => new List<Enum>();

        public bool CanBeNew { get; set; }

        //public Action<T, object[]?> Execute { get; set; } (inherited)
        public Func<T, string?>? CanExecute { get; set; }

        public Expression<Func<T, string?>>? CanExecuteExpression { get; set; }

        LambdaExpression? IOperation.CanExecuteExpression()
        {
            return CanExecuteExpression;
        }

        public Execute OverrideCanExecute(Overrider<Func<T, string?>> overrider)
        {
            CanExecute = overrider(CanExecute ?? (t => null));
            return this;
        }

        public void OverrideExecute(Overrider<Action<T, object?[]?>> overrider)
        {
            Execute = overrider(Execute);
        }

        public Execute(ExecuteSymbol<T> symbol)
        {
            this.executeSymbol = symbol ?? throw AutoInitAttribute.ArgumentNullException(typeof(ExecuteSymbol<T>), nameof(symbol));
            this.operationSymbol = symbol.Symbol;
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
            var currentUser = UserHolder.Current?.User!;
            using (var trace = HeavyProfiler.Log("Execute", new StructuredLogMessage("{Operation} on {Entity} done by {User}", this.operationSymbol.Key, entity, currentUser)))
            {
                if (trace?.activity is { } ac)
                {
                    ac.SetTag("Operation", this.executeSymbol.Symbol.Key);
                    ac.SetTag("Entity", entity);
                    ac.SetTag("User", currentUser);
                }
                OperationLogic.AssertOperationAllowed(executeSymbol.Symbol, entity.GetType(), inUserInterface: false, entity: (Entity)entity);

                OperationLogEntity log = new OperationLogEntity
                {
                    Operation = executeSymbol.Symbol,
                    Start = Clock.Now,
                    User = UserHolder.Current?.User!
                };

                try
                {
                    using (var tr = new Transaction())
                    {
                        using (OperationLogic.AllowSave(entity.GetType()))
                        {
                            var assertEnd = AssertEntity((T)entity);
                            OperationLogic.OnSuroundOperation(this, log, entity, args).EndUsing(_ =>
                            {
                                string? error = OnCanExecute((T)entity);
                                if (error != null)
                                {
                                    var ex = new ApplicationException(error);
                                    throw ex;
                                }

                                Execute((T)entity, args);

                                assertEnd?.Invoke();

                                if (!AvoidImplicitSave)
                                    entity.Save(); //Nothing happens if already saved
                                else if (ForReadonlyEntity && GraphExplorer.IsGraphModified((Entity)entity))
                                    throw new InvalidOperationException("Entity is modified but ForReadonlyEntity is true");
                                    

                                log.SetTarget(entity);
                                log.End = Clock.Now;
                            });
                        }

                        log.SaveLog();

                        tr.Commit();
                    }

                }
                catch (Exception ex)
                {
                    OperationLogic.SetExceptionData(ex, executeSymbol.Symbol, (Entity)entity, args);

                    if (Transaction.InTestTransaction)
                        throw;

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
                throw new InvalidOperationException("Operation {0} does not have Execute initialized".FormatWith(executeSymbol));

            if (ForReadonlyEntity)
            {
                AvoidImplicitSave = true;

                if(CanBeModified)
                    throw new InvalidOperationException("Operation {0}: CanBeModified is not compatible with OnlyReadyEntity".FormatWith(executeSymbol));

                if (CanBeNew)
                    throw new InvalidOperationException("Operation {0}: CanBeNew is not compatible with OnlyReadyEntity".FormatWith(executeSymbol));
            }
        }

        public override string ToString()
        {
            return "{0} Execute on {1}".FormatWith(executeSymbol, typeof(T));
        }

    }

    public class Delete : _Delete<T>, IDeleteOperation
    {
        protected readonly DeleteSymbol<T> deleteSymbol;
        protected readonly OperationSymbol operationSymbol;
        OperationSymbol IOperation.OperationSymbol => operationSymbol;
        Type IOperation.OverridenType => typeof(T);
        OperationType IOperation.OperationType => OperationType.Delete;
        public bool CanBeModified { get; set; }
        Type? IOperation.ReturnType => null;
        IList? IOperation.UntypedFromStates => new List<Enum>();
        IList? IOperation.UntypedToStates => null;
        Type? IOperation.StateType => null;
        LambdaExpression? IOperation.GetStateExpression() => null;

        public bool CanBeNew { get { return false; } }

        Type IEntityOperation.BaseType { get { return deleteSymbol.BaseType; } }
        bool IEntityOperation.HasCanExecute { get { return CanDelete != null; } }

        //public Action<T, object[]?> Delete { get; set; } (inherited)
        public Func<T, string?>? CanDelete { get; set; }

        public Expression<Func<T, string?>>? CanDeleteExpression { get; set; }

        public Delete OverrideCanDelete(Overrider<Func<T, string?>> overrider)
        {
            CanDelete = overrider(CanDelete ?? (t => null));
            return this;
        }

        public void OverrideDelete(Overrider<Action<T, object?[]?>> overrider)
        {
            Delete = overrider(Delete);
        }

        public Delete(DeleteSymbol<T> symbol)
        {
            deleteSymbol = symbol ?? throw AutoInitAttribute.ArgumentNullException(typeof(DeleteSymbol<T>), nameof(symbol));
            operationSymbol = deleteSymbol.Symbol;
        }

        LambdaExpression? IOperation.CanExecuteExpression()
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
            var currentUser = UserHolder.Current?.User!;
            using (var trace = HeavyProfiler.Log("Delete", new StructuredLogMessage("{Operation} on {Entity} done by {User}", this.operationSymbol.Key, entity, currentUser)))
            {
                if (trace?.activity is { } ac)
                {
                    ac.SetTag("Operation", this.operationSymbol.Key);
                    ac.SetTag("Entity", entity);
                    ac.SetTag("User", currentUser);
                }
                OperationLogic.AssertOperationAllowed(operationSymbol, entity.GetType(), inUserInterface: false, entity: (Entity)entity);

                OperationLogEntity log = new OperationLogEntity
                {
                    Operation = operationSymbol,
                    Start = Clock.Now,
                    User = UserHolder.Current?.User!,
                    Target = entity.ToLite(),
                };

                using (OperationLogic.AllowSave(entity.GetType()))
                    OperationLogic.OnSuroundOperation(this, log, entity, args).EndUsing(_ =>
                    {
                        try
                        {
                            using (var tr = new Transaction())
                            {
                                string? error = OnCanDelete((T)entity);
                                if (error != null)
                                    throw new ApplicationException(error);

                                OnDelete((T)entity, args);

                                log.SetTarget(entity);
                                log.End = Clock.Now;

                                log.SaveLog();

                                tr.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            OperationLogic.SetExceptionData(ex, operationSymbol, (Entity)entity, args);

                            if (Transaction.InTestTransaction)
                                throw;

                            var exLog = ex.LogException();

                            using (var tr2 = Transaction.ForceNew())
                            {
                                OperationLogEntity newLog = new OperationLogEntity //Transaction has been rollbacked
                                {
                                    Operation = log.Operation,
                                    Start = log.Start,
                                    User = log.User,
                                    End = Clock.Now,
                                    Target = log.Target,
                                    Exception = exLog.ToLite(),
                                };
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
                throw new InvalidOperationException("Operation {0} does not have Delete initialized".FormatWith(operationSymbol));
        }

        public override string ToString()
        {
            return "{0} Delete {1}".FormatWith(operationSymbol, typeof(T));
        }
    }
}
