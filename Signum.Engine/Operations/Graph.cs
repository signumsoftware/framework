using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Basics;
using Signum.Engine.Operations.Internal;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Engine.Operations
{
    public class Graph<T>
         where T : class, IIdentifiable
    {
        public class Construct : _Construct<T>, IConstructOperation
        {
            protected readonly Enum key;
            Enum IOperation.Key { get { return key; } }
            Type IOperation.Type { get { return typeof(T); } }
            OperationType IOperation.OperationType { get { return OperationType.Constructor; } }
            bool IOperation.Returns { get { return true; } }
            Type IOperation.ReturnType { get { return typeof(T); } }

            //public Func<object[], T> Construct { get; set; } (inherited)
            public bool Lite { get { return false; } }


            public Construct(Enum key)
            {
                this.key = key;
            }

            IIdentifiable IConstructOperation.Construct(params object[] args)
            {
                using (HeavyProfiler.Log("Construct", () => OperationDN.UniqueKey(key)))
                {
                    OperationLogic.AssertOperationAllowed(key, inUserInterface: false);

                    using (OperationLogic.AllowSave<T>())
                    {
                        try
                        {
                            using (Transaction tr = new Transaction())
                            {
                                OperationLogDN log = new OperationLogDN
                                {
                                    Operation = MultiEnumLogic<OperationDN>.ToEntity(key),
                                    Start = TimeZoneManager.Now,
                                    User = UserHolder.Current.ToLite()
                                };

                                OnBeginOperation();

                                T entity = Construct(args);

                                OnEndOperation(entity);

                                if (!entity.IsNew)
                                {
                                    log.Target = entity.ToLite();
                                    log.End = TimeZoneManager.Now;
                                    using (ExecutionMode.Global())
                                        log.Save();
                                }

                                return tr.Commit(entity);
                            }
                        }
                        catch (Exception ex)
                        {
                            OperationLogic.OnErrorOperation(this, null, args, ex);
                            throw;
                        }
                    }
                }
            }

            protected virtual void OnBeginOperation()
            {
                OperationLogic.OnBeginOperation(this, null);
            }

            protected virtual void OnEndOperation(T entity)
            {
                OperationLogic.OnEndOperation(this, entity);
            }

            public virtual void AssertIsValid()
            {
                if (Construct == null)
                    throw new InvalidOperationException("Operation {0} does not have Constructor initialized".Formato(key));
            }

            public override string ToString()
            {
                return "{0} Construct {1}".Formato(key, typeof(T));
            }
        }

        public class ConstructFrom<F> : IConstructorFromOperation
            where F : class, IIdentifiable
        {
            protected readonly Enum key;
            Enum IOperation.Key { get { return key; } }
            Type IOperation.Type { get { return typeof(F); } }
            OperationType IOperation.OperationType { get { return OperationType.ConstructorFrom; } }

            public bool Lite { get; set; }
            bool IOperation.Returns { get { return true; } }
            Type IOperation.ReturnType { get { return typeof(T); } }

            bool IEntityOperation.HasCanExecute { get { return CanConstruct != null; } }

            public bool AllowsNew { get; set; }

            public Func<F, object[], T> Construct { get; set; }
            public Func<F, string> CanConstruct { get; set; }

            public ConstructFrom(Enum key)
            {
                this.key = key;
                this.Lite = true;
            }

            string IEntityOperation.CanExecute(IIdentifiable entity)
            {
                return OnCanConstruct(entity);
            }

            string OnCanConstruct(IIdentifiable entity)
            {
                if (entity.IsNew && !AllowsNew)
                    return EngineMessage.TheEntity0IsNew.NiceToString().Formato(entity);

                if (CanConstruct != null)
                    return CanConstruct((F)entity);

                return null;
            }

            IIdentifiable IConstructorFromOperation.Construct(IIdentifiable entity, params object[] args)
            {
                using (HeavyProfiler.Log("ConstructFrom", () => OperationDN.UniqueKey(key)))
                {
                    OperationLogic.AssertOperationAllowed(key, inUserInterface: false);

                    string error = OnCanConstruct(entity);
                    if (error != null)
                        throw new ApplicationException(error);

                    using (OperationLogic.AllowSave(entity.GetType()))
                    using (OperationLogic.AllowSave<T>())
                    {
                        try
                        {
                            using (Transaction tr = new Transaction())
                            {
                                OperationLogDN log = new OperationLogDN
                                {
                                    Operation = MultiEnumLogic<OperationDN>.ToEntity(key),
                                    Start = TimeZoneManager.Now,
                                    User = UserHolder.Current.ToLite()
                                };

                                OnBeginOperation((IdentifiableEntity)entity);

                                T result = Construct((F)entity, args);

                                OnEndOperation(result);

                                if (result != null && !result.IsNew)
                                    log.Target = result.ToLite();

                                log.End = TimeZoneManager.Now;
                                using (ExecutionMode.Global())
                                    log.Save();

                                return tr.Commit(result);
                            }
                        }
                        catch (Exception e)
                        {
                            OperationLogic.OnErrorOperation(this, (IdentifiableEntity)entity, args, e);
                            throw;
                        }
                    }
                }
            }

            protected virtual void OnBeginOperation(IdentifiableEntity entity)
            {
                OperationLogic.OnBeginOperation(this, entity);
            }

            protected virtual void OnEndOperation(T result)
            {
                OperationLogic.OnEndOperation(this, result);
            }


            public virtual void AssertIsValid()
            {
                if (Construct == null)
                    throw new InvalidOperationException("Operation {0} does not hace Construct initialized".Formato(key));
            }

            public override string ToString()
            {
                return "{0} ConstructFrom {1} -> {2}".Formato(key, typeof(F), typeof(T));
            }

        }

        public class ConstructFromMany<F> : IConstructorFromManyOperation
            where F : class, IIdentifiable
        {
            protected readonly Enum key;
            Enum IOperation.Key { get { return key; } }
            Type IOperation.Type { get { return typeof(F); } }
            OperationType IOperation.OperationType { get { return OperationType.ConstructorFromMany; } }

            bool IOperation.Returns { get { return true; } }
            Type IOperation.ReturnType { get { return typeof(T); } }

            public Func<List<Lite<F>>, object[], T> Construct { get; set; }

            public ConstructFromMany(Enum key)
            {
                this.key = key;
            }

            IIdentifiable IConstructorFromManyOperation.Construct(IEnumerable<Lite<IIdentifiable>> lites, params object[] args)
            {
                using (HeavyProfiler.Log("ConstructFromMany", () => OperationDN.UniqueKey(key)))
                {
                    OperationLogic.AssertOperationAllowed(key, inUserInterface: false);

                    using (OperationLogic.AllowSave<F>())
                    using (OperationLogic.AllowSave<T>())
                    {
                        try
                        {
                            using (Transaction tr = new Transaction())
                            {
                                OperationLogDN log = new OperationLogDN
                                {
                                    Operation = MultiEnumLogic<OperationDN>.ToEntity(key),
                                    Start = TimeZoneManager.Now,
                                    User = UserHolder.Current.ToLite()
                                };

                                OnBeginOperation();

                                T result = OnConstruct(lites.Cast<Lite<F>>().ToList(), args);

                                OnEndOperation(result);

                                if (result != null && !result.IsNew)
                                    log.Target = result.ToLite();

                                log.End = TimeZoneManager.Now;
                                using (ExecutionMode.Global())
                                    log.Save();

                                return tr.Commit(result);
                            }
                        }
                        catch (Exception e)
                        {
                            OperationLogic.OnErrorOperation(this, null, args, e);
                            throw;
                        }
                    }
                }
            }

            protected virtual void OnBeginOperation()
            {
                OperationLogic.OnBeginOperation(this, null);
            }

            protected virtual void OnEndOperation(T result)
            {
                OperationLogic.OnEndOperation(this, result);
            }

            protected virtual T OnConstruct(List<Lite<F>> lites, object[] args)
            {
                return Construct(lites, args);
            }

            public virtual void AssertIsValid()
            {
                if (Construct == null)
                    throw new InvalidOperationException("Operation {0} Constructor initialized".Formato(key));
            }

            public override string ToString()
            {
                return "{0} ConstructFromMany {1} -> {2}".Formato(key, typeof(F), typeof(T));
            }

        }

        public class Execute : _Execute<T>, IExecuteOperation
        {
            protected readonly Enum key;
            Enum IOperation.Key { get { return key; } }
            Type IOperation.Type { get { return typeof(T); } }
            OperationType IOperation.OperationType { get { return OperationType.Execute; } }
            public bool Lite { get; set; }
            bool IOperation.Returns { get { return true; } }
            Type IOperation.ReturnType { get { return null; } }

            bool IEntityOperation.HasCanExecute { get { return CanExecute != null; } }

            public bool AllowsNew { get; set; }

            //public Action<T, object[]> Execute { get; set; } (inherited)
            public Func<T, string> CanExecute { get; set; }

            public Execute(Enum key)
            {
                this.key = key;
                this.Lite = true;
            }

            string IEntityOperation.CanExecute(IIdentifiable entity)
            {
                return OnCanExecute((T)entity);
            }

            protected virtual string OnCanExecute(T entity)
            {
                if (entity.IsNew && !AllowsNew)
                    return EngineMessage.TheEntity0IsNew.NiceToString().Formato(entity);

                if (CanExecute != null)
                    return CanExecute(entity);

                return null;
            }

            void IExecuteOperation.Execute(IIdentifiable entity, params object[] parameters)
            {
                using (HeavyProfiler.Log("Execute", () => OperationDN.UniqueKey(key)))
                {
                    OperationLogic.AssertOperationAllowed(key, inUserInterface: false);

                    string error = OnCanExecute((T)entity);
                    if (error != null)
                        throw new ApplicationException(error);

                    OperationLogDN log = new OperationLogDN
                    {
                        Operation = MultiEnumLogic<OperationDN>.ToEntity(key),
                        Start = TimeZoneManager.Now,
                        User = UserHolder.Current.ToLite()
                    };

                    using (OperationLogic.AllowSave(entity.GetType()))
                    {
                        try
                        {
                            using (Transaction tr = new Transaction())
                            {
                                OnBeginOperation((T)entity);

                                Execute((T)entity, parameters);

                                OnEndOperation((T)entity);

                                entity.Save(); //Nothing happens if already saved

                                log.Target = entity.ToLite(); //in case AllowsNew == true
                                log.End = TimeZoneManager.Now;
                                using (ExecutionMode.Global())
                                    log.Save();

                                tr.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            OperationLogic.OnErrorOperation(this, (IdentifiableEntity)entity, parameters, ex);

                            if (!entity.IsNew)
                            {
                                if (Transaction.InTestTransaction)
                                    throw;

                                var exLog = ex.LogException();

                                using (Transaction tr2 = Transaction.ForceNew())
                                {
                                    OperationLogDN log2 = new OperationLogDN
                                    {
                                        Operation = log.Operation,
                                        Start = log.Start,
                                        User = log.User,
                                        Target = entity.ToLite(),
                                        Exception = exLog.ToLite(),
                                        End = TimeZoneManager.Now
                                    };

                                    using (ExecutionMode.Global())
                                        log2.Save();

                                    tr2.Commit();
                                }
                            }
                            throw;
                        }
                    }
                }
            }

            protected virtual void OnBeginOperation(T entity)
            {
                OperationLogic.OnBeginOperation(this, entity);
            }

            protected virtual void OnEndOperation(T entity)
            {
                OperationLogic.OnEndOperation(this, entity);
            }

            public virtual void AssertIsValid()
            {
                if (Execute == null)
                    throw new InvalidOperationException("Operation {0} does not have Execute initialized".Formato(key));
            }

            public override string ToString()
            {
                return "{0} Execute on {1}".Formato(key, typeof(T));
            }
        }

        public class Delete : _Delete<T>, IDeleteOperation
        {
            protected readonly Enum key;
            Enum IOperation.Key { get { return key; } }
            Type IOperation.Type { get { return typeof(T); } }
            OperationType IOperation.OperationType { get { return OperationType.Delete; } }
            public bool Lite { get; set; }
            bool IOperation.Returns { get { return false; } }
            Type IOperation.ReturnType { get { return null; } }

            public bool AllowsNew { get { return false; } }

            bool IEntityOperation.HasCanExecute { get { return CanDelete != null; } }

            //public Action<T, object[]> Delete { get; set; } (inherited)
            public Func<T, string> CanDelete { get; set; }

            public Delete(Enum key)
            {
                this.key = key;
                this.Lite = true;
            }

            string IEntityOperation.CanExecute(IIdentifiable entity)
            {
                return OnCanDelete((T)entity);
            }

            protected virtual string OnCanDelete(T entity)
            {
                if (entity.IsNew)
                    return EngineMessage.TheEntity0IsNew.NiceToString().Formato(entity);

                if (CanDelete != null)
                    return CanDelete(entity);

                return null;
            }

            void IDeleteOperation.Delete(IIdentifiable entity, params object[] parameters)
            {
                using (HeavyProfiler.Log("Delete", () => OperationDN.UniqueKey(key)))
                {
                    OperationLogic.AssertOperationAllowed(key, inUserInterface: false);

                    string error = OnCanDelete((T)entity);
                    if (error != null)
                        throw new ApplicationException(error);

                    OperationLogDN log = new OperationLogDN
                    {
                        Operation = MultiEnumLogic<OperationDN>.ToEntity(key),
                        Start = TimeZoneManager.Now,
                        User = UserHolder.Current.ToLite()
                    };

                    using (OperationLogic.AllowSave(entity.GetType()))
                    {
                        try
                        {
                            using (Transaction tr = new Transaction())
                            {
                                OperationLogic.OnBeginOperation(this, (IdentifiableEntity)entity);

                                OnDelete((T)entity, parameters);

                                OperationLogic.OnEndOperation(this, (IdentifiableEntity)entity);

                                log.Target = entity.ToLite(); //in case AllowsNew == true
                                log.End = TimeZoneManager.Now;
                                using (ExecutionMode.Global())
                                    log.Save();

                                tr.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            OperationLogic.OnErrorOperation(this, (IdentifiableEntity)entity, parameters, ex);

                            if (Transaction.InTestTransaction)
                                throw;

                            var exLog = ex.LogException();

                            using (Transaction tr2 = Transaction.ForceNew())
                            {
                                var log2 = new OperationLogDN
                                {
                                    Operation = log.Operation,
                                    Start = log.Start,
                                    End = TimeZoneManager.Now,
                                    Target = entity.ToLite(),
                                    Exception = exLog.ToLite(),
                                    User = log.User
                                };

                                using (ExecutionMode.Global())
                                    log2.Save();

                                tr2.Commit();
                            }

                            throw;
                        }
                    }
                }
            }

            protected virtual void OnDelete(T entity, object[] args)
            {
                Delete(entity, args);
            }


            public virtual void AssertIsValid()
            {
                if (Delete == null)
                    throw new InvalidOperationException("Operation {0} does not have Delete initialized".Formato(key));
            }

            public override string ToString()
            {
                return "{0} Delete {1}".Formato(key, typeof(T));
            }
        }
    }
}
