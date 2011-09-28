using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Operations;
using Signum.Entities.Authorization;
using System.Threading;
using Signum.Utilities;
using Signum.Engine.Basics;
using Signum.Engine.Extensions.Properties;
using Signum.Engine.Authorization;

namespace Signum.Engine.Operations
{
    public interface IExecuteOperation : IEntityOperation
    {
        void Execute(IIdentifiable entity, params object[] parameters);
    }

    public class BasicExecute<T> : IExecuteOperation
      where T : class, IIdentifiable
    {
        public Enum Key { get; private set; }
        public Type Type { get { return typeof(T); } }
        public OperationType OperationType { get { return OperationType.Execute; } }
        public bool Lite { get; set; }
        public bool Returns { get; set; }
        public Type ReturnType { get { return null; } }

        public bool AllowsNew { get; set; }

        public Action<T, object[]> Execute { get; set; }
        public Func<T, string> CanExecute { get; set; }

        public BasicExecute(Enum key)
        {
            this.Key = key;
            this.Lite = true;
            this.Returns = true;
        }

        string IEntityOperation.CanExecute(IIdentifiable entity)
        {
            return OnCanExecute((T)entity);
        }

        protected virtual string OnCanExecute(T entity)
        {
            if (entity.IsNew && !AllowsNew)
                return Resources.TheEntity0IsNew.Formato(entity);

            if (CanExecute != null)
                return CanExecute(entity);
            
            return null;
        }

        void IExecuteOperation.Execute(IIdentifiable entity, params object[] parameters)
        {
            OperationLogic.AssertOperationAllowed(Key);

            string error = OnCanExecute((T)entity);
            if (error != null)
                throw new ApplicationException(error);

            LogOperationDN log = new LogOperationDN
            {
                Operation = EnumLogic<OperationDN>.ToEntity(Key),
                Start = TimeZoneManager.Now,
                User = UserDN.Current.ToLite()
            };

            using (OperationLogic.AllowSave<T>())
            {
                try
                {
                    using (Transaction tr = new Transaction())
                    {
                        OnBeginOperation((T)entity);

                        Execute((T)entity, parameters);

                        OnEndOperation((T)entity);

                        entity.Save(); //Nothing happens if already saved

                        log.Target = entity.ToLite<IIdentifiable>(); //in case AllowsNew == true
                        log.End = TimeZoneManager.Now;
                        using (AuthLogic.User(AuthLogic.SystemUser))
                            log.Save();

                        tr.Commit();
                    }
                }
                catch (Exception ex)
                {
                    OperationLogic.OnErrorOperation(this, (IdentifiableEntity)entity, ex);

                    if (!entity.IsNew)
                    {
                        try
                        {
                            using (Transaction tr2 = new Transaction(true))
                            {
                                log.Target = entity.ToLite<IIdentifiable>();
                                log.Exception = ex.Message;
                                log.End = TimeZoneManager.Now;

                                using (AuthLogic.User(AuthLogic.SystemUser))
                                    log.Save();

                                tr2.Commit();
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    throw ex;
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
                throw new InvalidOperationException("Operation {0} does not have Execute initialized".Formato(Key));
        }

        public override string ToString()
        {
            return "{0} Execute on {1}".Formato(Key, typeof(T));
        }
    }
}
