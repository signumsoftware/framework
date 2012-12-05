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
using Signum.Engine.Exceptions;

namespace Signum.Engine.Operations
{
    public interface IDeleteOperation : IEntityOperation
    {
        void Delete(IIdentifiable entity, params object[] parameters);
    }

    public class BasicDelete<T> : IDeleteOperation
      where T : class, IIdentifiable
    {
        protected readonly Enum key;
        Enum IOperation.Key { get { return key; } }
        Type IOperation.Type { get { return typeof(T); } }
        OperationType IOperation.OperationType { get { return OperationType.Delete; } }
        public bool Lite { get; set; }
        bool IOperation.Returns { get { return false; } }
        Type IOperation.ReturnType { get { return null; } }

        public bool AllowsNew { get { return false; } }

        public Action<T, object[]> Delete { get; set; }
        public Func<T, string> CanDelete { get; set; }

        public BasicDelete(Enum key)
        {
            this.key = key;
            this.Lite = true;
        }

        string IEntityOperation.CanExecute(IIdentifiable entity)
        {
            return OnCanDelete((T)entity) ;
        }

        protected virtual string OnCanDelete(T entity)
        {
            if (entity.IsNew)
                return Resources.TheEntity0IsNew.Formato(entity);

            if (CanDelete != null)
                return CanDelete(entity);
            
            return null;
        }

        void IDeleteOperation.Delete(IIdentifiable entity, params object[] parameters)
        {
            OperationLogic.AssertOperationAllowed(key, inUserInterface: false);

            string error = OnCanDelete((T)entity);
            if (error != null)
                throw new ApplicationException(error);

            OperationLogDN log = new OperationLogDN
            {
                Operation = MultiEnumLogic<OperationDN>.ToEntity(key),
                Start = TimeZoneManager.Now,
                User = UserDN.Current.ToLite()
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

                        log.Target = entity.ToLite<IIdentifiable>(); //in case AllowsNew == true
                        log.End = TimeZoneManager.Now;
                        using (AuthLogic.Disable())
                            log.Save();

                        tr.Commit();
                    }
                }
                catch (Exception ex)
                {
                    OperationLogic.OnErrorOperation(this, (IdentifiableEntity)entity, ex);

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
                            Target = entity.ToLite<IIdentifiable>(),
                            Exception = exLog.ToLite(),
                            User = log.User
                        };

                        using (AuthLogic.Disable())
                            log2.Save();

                        tr2.Commit();
                    }

                    throw;
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
