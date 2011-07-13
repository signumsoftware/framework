using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using System.Threading;
using Signum.Utilities;
using Signum.Engine.Basics;
using Signum.Engine.Extensions.Properties;
using Signum.Engine.Authorization;

namespace Signum.Engine.Operations
{
    public interface IConstructOperation : IOperation
    {
        IIdentifiable Construct(params object[] parameters);
    }

    public class BasicConstruct<T> : IConstructOperation
        where T: IIdentifiable
    {
        public Enum Key { get; private set; }        
        public Type Type { get { return typeof(T); } }
        public OperationType OperationType { get { return OperationType.Constructor; } }
        public bool Returns { get { return true; } }
        public Type ReturnType { get { return typeof(T); } }

        public bool Lite { get { return false; } }
        public Func<object[], T> Construct { get; set; }

        public BasicConstruct(Enum key)
        {
            this.Key = key; 
        }

        IIdentifiable IConstructOperation.Construct(params object[] args)
        {
            OperationLogic.AssertOperationAllowed(Key);
             
             try
             {

                 using (Transaction tr = new Transaction())
                 {
                     LogOperationDN log = new LogOperationDN
                     {
                         Operation = EnumLogic<OperationDN>.ToEntity(Key),
                         Start = TimeZoneManager.Now,
                         User = UserDN.Current.ToLite()
                     };

                     OperationLogic.OnBeginOperation(this, null);

                     T entity = OnConstruct(args);

                     OperationLogic.OnEndOperation(this, entity);

                     if (!entity.IsNew)
                     {
                         log.Target = entity.ToLite<IIdentifiable>();
                         log.End = TimeZoneManager.Now;
                         using (AuthLogic.User(AuthLogic.SystemUser))
                             log.Save();
                     }

                     return tr.Commit(entity);
                 }
             }
             catch (Exception ex)
             {
                 OperationLogic.OnErrorOperation(this, null, ex);
                 throw;
             }
        }

        protected virtual T OnConstruct(object[] args)
        {
            return Construct(args);
        }

        public virtual void AssertIsValid()
        {
            if (Construct == null)
                throw new InvalidOperationException("Operation {0} does not have Constructor initialized".Formato(Key));
        }

        public override string ToString()
        {
            return "{0} Construct {1}".Formato(Key, typeof(T));
        }
    }
}
