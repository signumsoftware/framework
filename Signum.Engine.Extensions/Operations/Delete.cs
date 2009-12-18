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

namespace Signum.Engine.Operations
{
    public interface IDeleteOperation : IEntityOperation
    {
        void Delete(IIdentifiable entity, params object[] parameters);
    }

    public class BasicDelete<T> : IDeleteOperation
      where T : class, IIdentifiable
    {
        public Enum Key { get; private set; }
        public Type Type { get { return typeof(T); } }
        public OperationType OperationType { get { return OperationType.Delete; } }
        public bool Lite { get { return true; } }
        public bool Returns { get { return false; } }
        public Type ReturnType { get { return null; } }

        public bool AllowsNew { get { return false; } }

        public Action<T, object[]> Delete { get; set; }
        public Func<T, string> CanDelete { get; set; }

        public BasicDelete(Enum key)
        {
            this.Key = key;
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
            string error = OnCanDelete((T)entity);
            if (error != null)
                throw new ApplicationException(error);

            if (!OperationLogic.OnAllowOperation(Key))
                throw new UnauthorizedAccessException(Resources.Operation0IsNotAuthorized.Formato(Key)); 

            try
            {
                using (Transaction tr = new Transaction())
                {
                    LogOperationDN log = new LogOperationDN
                    {
                        Operation = EnumLogic<OperationDN>.ToEntity(Key),
                        Start = DateTime.Now,
                        User = UserDN.Current
                    };

                    OperationLogic.OnBeginOperation(this, (IdentifiableEntity)entity);

                    OnDelete((T)entity, parameters);

                    OperationLogic.OnEndOperation(this, (IdentifiableEntity)entity);

                    log.Target = entity.ToLite<IIdentifiable>(); //in case AllowsNew == true
                    log.End = DateTime.Now;
                    log.Save();

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {  
                OperationLogic.OnErrorOperation(this, (IdentifiableEntity)entity, ex);    

                try
                {
                    using (Transaction tr2 = new Transaction(true))
                    {
                        new LogOperationDN
                        {
                            Operation = EnumLogic<OperationDN>.ToEntity(Key),
                            Start = DateTime.Now,
                            Target = entity.ToLite<IIdentifiable>(),
                            Exception = ex.Message,
                            User = UserDN.Current
                        }.Save();

                        tr2.Commit();
                    }
                }
                catch (Exception)
                { 
                }

                throw ex;
            }
        }

        protected virtual void OnDelete(T entity, object[] args)
        {
            Delete(entity, args); 
        }


        public void AssertIsValid()
        {
            if (Delete == null)
                throw new ApplicationException(Resources.Operation0DoesNotHaveDeleteInitialized.Formato(Key));
        }
    }
}
