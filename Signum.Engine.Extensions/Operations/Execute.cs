using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Operations;
using Signum.Entities.Authorization;
using System.Threading;
using Signum.Utilities;

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
        public bool Lazy { get; set; }
        public bool Returns { get; set; }

        public Func<T, string> CanExecute { get; set; }
        public Action<T, object[]> Execute { get; set; }

        public BasicExecute(Enum key)
        {
            this.Key = key;
            this.Lazy = true;
            this.Returns = true;
        }

        bool IEntityOperation.CanExecute(IIdentifiable entity)
        {
            return OnCanExecute((T)entity) == null;
        }

        protected virtual string OnCanExecute(T entity)
        {
            if (entity.IsNew)
                return "The Entity {0} is New".Formato(entity);

            if (CanExecute != null)
                return CanExecute(entity);
            
            return null;
        }

        void IExecuteOperation.Execute(IIdentifiable entity, params object[] parameters)
        {
            string error = OnCanExecute((T)entity);
            if (error != null)
                throw new ApplicationException(error);

            try
            {
                using (Transaction tr = new Transaction())
                {
                    LogOperationDN log = new LogOperationDN
                    {
                        Operation = OperationLogic.ToOperation[Key],
                        Start = DateTime.Now,
                        Entity = ((IdentifiableEntity)entity).ToLazy(),
                        User = UserDN.Current
                    };

                    Execute((T)entity, parameters);

                    ((IdentifiableEntity)entity).Save(); //Nothing happens if allready saved

                    log.End = DateTime.Now;
                    log.Save();

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    using (Transaction tr2 = new Transaction(true))
                    {
                        new LogOperationDN
                        {
                            Operation = OperationLogic.ToOperation[Key],
                            Start = DateTime.Now,
                            Entity = ((IdentifiableEntity)entity).ToLazy(),
                            Exception = ex.Message,
                            User = (UserDN)Thread.CurrentPrincipal
                        }.Save();

                        tr2.Commit();
                    }
                }
                catch (Exception) { }

                throw ex;
            }
        }

        protected virtual void OnExecute(T entity, object[] args)
        {
            Execute(entity, args); 
        }


        public void AssertIsValid()
        {
            if (Execute == null)
                throw new ApplicationException("Operation {0} does not have Execute initialized".Formato(Key));
        }
    }
}
